Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports rng = Microsoft.VisualBasic.Math.RandomExtensions

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v2.0 改进] 扩散规则
    ''' 
    ''' 改进：
    ''' 1. 代谢中间产物（pyruvate, acetate, lactate）参与扩散——交叉喂养的前提
    ''' 2. 信号分子、抗生素、铁载体也参与扩散
    ''' 3. 扩散速率与浓度差成正比
    ''' 4. 生物膜部分阻断扩散
    ''' 5. ExecuteEnvironment方法用于环境级别扩散
    ''' 
    ''' [v3.0 改进] 扩散规则
    ''' 
    ''' v3.0改进：
    ''' 1. 新增代谢物参与扩散：succinate, ethanol, formate, butyrate, fatty acid, methane
    ''' 2. 新增离子扩散：Na+, K+, Cl-, phosphate, sulfate, Fe2+/Fe3+
    ''' 3. 新增次级代谢物扩散：vitamin, pigment, toxin, compatible solute
    ''' 4. 热量扩散由TemperatureRule处理
    ''' </summary>
    Public Class DiffusionRule : Implements IEnvironmentRule

        ''' <summary>
        ''' 环境级别的扩散计算
        ''' </summary>
        Public Sub ExecuteEnvironment(env As NaturalEnvironment, iteration As Long) Implements IEnvironmentRule.ExecuteEnvironment
            Dim dims = env.Dimensions
            Dim diffusable = {
                              _ ' 基础
                MoleculeType.Oxygen, MoleculeType.Water, MoleculeType.HydrogenIon,
                MoleculeType.HydroxideIon, MoleculeType.CarbonDioxide,
                MoleculeType.CarbonSource, MoleculeType.NitrogenSource,
                                                                       _ ' 核心碳代谢
                MoleculeType.Glucose, MoleculeType.Pyruvate, MoleculeType.Acetate,
                MoleculeType.Lactate,
                                     _ ' [v3.0] 扩展碳代谢
                MoleculeType.Succinate, MoleculeType.Ethanol, MoleculeType.Formate,
                MoleculeType.Butyrate, MoleculeType.FattyAcid, MoleculeType.Methane,
                                                                                    _ ' [v3.0] 离子
                MoleculeType.SodiumIon, MoleculeType.PotassiumIon, MoleculeType.ChlorideIon,
                MoleculeType.Phosphate, MoleculeType.Sulfate, MoleculeType.Sulfide,
                MoleculeType.IronII, MoleculeType.IronIII,
                MoleculeType.CalciumIon, MoleculeType.MagnesiumIon,
                                                                   _ ' [v3.0] 扩展氨基酸
                MoleculeType.AminoMixAromatic, MoleculeType.AminoMixBranched, MoleculeType.AminoMixThiol,
                                                                                                         _ ' 信号/防御
                MoleculeType.SignalMolecule, MoleculeType.Antibiotic,
                MoleculeType.Siderophore, MoleculeType.SecondaryMetabolite,
                                                                           _ ' [v3.0] 次级代谢
                MoleculeType.Vitamin, MoleculeType.Pigment, MoleculeType.Toxin,
                MoleculeType.CompatibleSolute
            }

            For x As Integer = 0 To dims.Width - 1
                For y As Integer = 0 To dims.Height - 1
                    For z As Integer = 0 To dims.Depth - 1
                        Dim voxel = env.Grid(x, y, z)
                        Dim neighbors = env.GetNeighbors(voxel)

                        For Each neighbor In neighbors
                            ' 生物膜阻断扩散（强度越高阻断越多）
                            Dim diffusionFactor = 1.0
                            If voxel.HasBiofilm Then
                                diffusionFactor *= Math.Max(0.1, 1.0 - voxel.BiofilmStrength / 100.0)
                            End If
                            If neighbor.HasBiofilm Then
                                diffusionFactor *= Math.Max(0.1, 1.0 - neighbor.BiofilmStrength / 100.0)
                            End If

                            For Each mol In diffusable
                                If voxel.ExternalMolecules.ContainsKey(mol) AndAlso
                                   neighbor.ExternalMolecules.ContainsKey(mol) Then

                                    Dim diff = voxel.ExternalMolecules(mol) - neighbor.ExternalMolecules(mol).Quantity
                                    If Math.Abs(diff) > 0 Then
                                        ' 生物膜阻断
                                        ' 扩散量 = 浓度差 × 扩散系数 × 生物膜因子
                                        Dim transfer = CInt(Math.Sign(diff) *
                                                     Math.Min(Math.Abs(diff) * 0.15 * diffusionFactor,
                                                              rng.NextInteger(1, 6)))

                                        If transfer <> 0 Then
                                            voxel.ExternalMolecules(mol).AddQuantity(-transfer)
                                            neighbor.ExternalMolecules(mol).AddQuantity(transfer)

                                            ' 确保不为负
                                            If voxel.ExternalMolecules(mol) < 0 Then
                                                neighbor.ExternalMolecules(mol).AddQuantity(voxel.ExternalMolecules(mol).Quantity)
                                                voxel.ExternalMolecules(mol).SetQuantity(0)
                                            End If
                                        End If
                                    End If
                                End If
                            Next
                        Next
                    Next
                Next
            Next
        End Sub
    End Class
End Namespace
