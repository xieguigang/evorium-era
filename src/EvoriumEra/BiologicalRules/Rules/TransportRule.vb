Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v2.0 改进] 物质运输规则
    ''' 
    ''' 改进：
    ''' 1. 内吞：主动摄取环境中的营养物质
    ''' 2. 分泌：主动排出代谢产物
    ''' 3. 溢流分泌：中间产物超出容量60%时自动分泌50%到环境
    '''    这是交叉喂养网络的关键驱动机制
    '''    
    ''' [v3.0 改进] 物质运输规则
    ''' 
    ''' v3.0改进：
    ''' 1. 内吞增加对扩展代谢物和离子的摄取
    ''' 2. 溢流分泌增加扩展代谢物
    ''' 3. 维生素摄取提升代谢效率
    ''' </summary>
    Public Class TransportRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(GeneOntology.Endocytosis, GeneOntology.Exocytosis)
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)

            ' ===== 物质内吞 =====
            If cell.HasFunction(GeneOntology.Endocytosis) Then
                ' 优先摄取的营养物质列表
                Dim priorityNutrients = {
                    MoleculeType.Glucose, MoleculeType.Pyruvate, MoleculeType.Acetate,
                    MoleculeType.Lactate, MoleculeType.CarbonSource, MoleculeType.NitrogenSource,
                    MoleculeType.Oxygen, MoleculeType.Water, MoleculeType.Phosphate, _
                                                                                     _ ' [v3.0] 扩展
                    MoleculeType.Succinate, MoleculeType.Ethanol, MoleculeType.Formate,
                    MoleculeType.Butyrate, MoleculeType.FattyAcid,
                    MoleculeType.AminoMixGluFamily, MoleculeType.AminoMixAspFamily,
                    MoleculeType.AminoMixSerGly,
                    MoleculeType.AminoMixAromatic, MoleculeType.AminoMixBranched, MoleculeType.AminoMixThiol,
                    MoleculeType.Vitamin, MoleculeType.Siderophore
                }

                For Each molType In priorityNutrients
                    Dim available = voxel.GetMoleculeAmount(molType)
                    If available > 0 Then
                        Dim uptake = Math.Min(available, 5)
                        cell.AddMoleculeInternal(molType, uptake)
                        If Not voxel.ExternalMolecules.ContainsKey(molType) Then
                            voxel.ExternalMolecules(molType) = 0
                        End If
                        voxel.ExternalMolecules(molType) -= uptake
                        If voxel.ExternalMolecules(molType) < 0 Then voxel.ExternalMolecules(molType) = 0
                    End If
                Next

                If ConsumeBasicResources(cell) Then
                    ' 内吞消耗已在ConsumeBasicResources中处理
                End If
            End If

            ' ===== 溢流分泌 =====
            Dim capacity = 10000
            ' ===== 代谢溢流分泌（核心交叉喂养机制）=====
            ' 当中间产物超出细胞容量的60%时，自动分泌50%到环境
            ' 这确保了代谢中间产物（pyruvate, acetate, lactate）能被其他细胞利用
            Dim overflowMolecules = {
                MoleculeType.Pyruvate, MoleculeType.Acetate, MoleculeType.Lactate,
                MoleculeType.Succinate, MoleculeType.Ethanol, MoleculeType.Formate,
                MoleculeType.Butyrate, MoleculeType.FattyAcid, MoleculeType.Methane,
                MoleculeType.AminoMixGluFamily, MoleculeType.AminoMixAspFamily, MoleculeType.AminoMixSerGly,
                MoleculeType.AminoMixAromatic, MoleculeType.AminoMixBranched, MoleculeType.AminoMixThiol,
                MoleculeType.Vitamin, MoleculeType.Pigment, MoleculeType.Toxin,
                MoleculeType.Antibiotic, MoleculeType.SecondaryMetabolite
            }

            For Each mol In overflowMolecules
                Dim amount = cell.GetMoleculeAmount(mol)
                If amount > capacity * 0.6 Then
                    Dim secretion = CInt(amount * 0.5)
                    If secretion > 0 Then
                        cell.AddMoleculeInternal(mol, -secretion)
                        env.AddMolecule(cell, mol, secretion)
                    End If
                End If
            Next

            ' ===== 主动分泌 =====
            If cell.HasFunction(GeneOntology.Exocytosis) Then
                ' 分泌信号分子
                Dim signal = cell.GetMoleculeAmount(MoleculeType.SignalMolecule)
                If signal > 5 Then
                    Dim secrete = signal \ 2
                    cell.AddMoleculeInternal(MoleculeType.SignalMolecule, -secrete)
                    env.AddMolecule(cell, MoleculeType.SignalMolecule, secrete)
                End If

                ' 分泌铁载体
                Dim siderophore = cell.GetMoleculeAmount(MoleculeType.Siderophore)
                If siderophore > 3 Then
                    cell.AddMoleculeInternal(MoleculeType.Siderophore, -siderophore)
                    env.AddMolecule(cell, MoleculeType.Siderophore, siderophore)
                End If

                ' [v3.0] 分泌维生素（交叉喂养）
                Dim vitamin = cell.GetMoleculeAmount(MoleculeType.Vitamin)
                If vitamin > 3 Then
                    Dim secrete = vitamin \ 2
                    cell.AddMoleculeInternal(MoleculeType.Vitamin, -secrete)
                    env.AddMolecule(cell, MoleculeType.Vitamin, secrete)
                End If
            End If

            ' [v3.0] 维生素效果：有维生素时ATP产率提升
            Dim vit = cell.GetMoleculeAmount(MoleculeType.Vitamin)
            If vit > 0 Then
                cell.ATP = Math.Min(cell.ATP + 1, 1000)
                cell.AddMoleculeInternal(MoleculeType.Vitamin, -1) ' 维生素被消耗
            End If

            ' [v3.0] 色素效果：有色素时耐热性提升（在TemperatureRule中处理）
        End Sub
    End Class
End Namespace
