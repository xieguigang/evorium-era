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
                    MoleculeType.Oxygen, MoleculeType.Water,
                    MoleculeType.AminoMixGluFamily, MoleculeType.AminoMixAspFamily, MoleculeType.AminoMixSerGly,
                    MoleculeType.Nucleotide, MoleculeType.Siderophore
                }

                For Each moleculeType In priorityNutrients
                    If voxel.ExternalMolecules.ContainsKey(moleculeType) Then
                        Dim available = voxel.ExternalMolecules(moleculeType)
                        If available > 0 Then
                            ' 根据细胞需求决定摄取量
                            Dim currentLevel = cell.GetMoleculeAmount(moleculeType)
                            Dim uptakeAmount As Integer

                            ' 缺乏时多摄取，充足时少摄取
                            If currentLevel < 10 Then
                                uptakeAmount = Math.Min(available, 10)
                            ElseIf currentLevel < 50 Then
                                uptakeAmount = Math.Min(available, 5)
                            Else
                                uptakeAmount = Math.Min(available, 2)
                            End If

                            If uptakeAmount > 0 Then
                                voxel.ExternalMolecules(moleculeType) -= uptakeAmount
                                cell.AddMoleculeInternal(moleculeType, uptakeAmount)

                                If ConsumeBasicResources(cell) Then
                                    ' 运输消耗ATP已在ConsumeBasicResources中处理
                                End If
                            End If
                        End If
                    End If
                Next
            End If

            ' ===== 代谢溢流分泌（核心交叉喂养机制）=====
            ' 当中间产物超出细胞容量的60%时，自动分泌50%到环境
            ' 这确保了代谢中间产物（pyruvate, acetate, lactate）能被其他细胞利用
            Dim overflowMolecules = {
                MoleculeType.Pyruvate, MoleculeType.Acetate, MoleculeType.Lactate,
                MoleculeType.CarbonDioxide, MoleculeType.HydrogenIon,
                MoleculeType.SecondaryMetabolite, MoleculeType.SignalMolecule
            }

            Dim capacityThreshold = 6000 ' 60% of 10000

            For Each mol In overflowMolecules
                Dim amount = cell.GetMoleculeAmount(mol)
                If amount > 100 Then ' 超过100个单位就考虑分泌
                    Dim excessFraction = 0.5
                    Dim secretion = CInt(amount * excessFraction)

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
            End If
        End Sub
    End Class
End Namespace
