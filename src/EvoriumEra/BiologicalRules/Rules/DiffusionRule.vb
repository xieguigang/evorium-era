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
    ''' </summary>
    Public Class DiffusionRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New() ' 全局规则，不绑定特定基因功能
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            ' 细胞级别的扩散由ExecuteEnvironment处理
        End Sub

        ''' <summary>
        ''' 环境级别的扩散计算
        ''' </summary>
        Public Sub ExecuteEnvironment(env As NaturalEnvironment)
            Dim diffusable = {
                MoleculeType.Oxygen, MoleculeType.Water, MoleculeType.HydrogenIon,
                MoleculeType.HydroxideIon, MoleculeType.CarbonDioxide,
                MoleculeType.CarbonSource, MoleculeType.NitrogenSource,
                MoleculeType.Glucose, MoleculeType.Pyruvate, MoleculeType.Acetate,
                MoleculeType.Lactate, MoleculeType.SignalMolecule,
                MoleculeType.Antibiotic, MoleculeType.Siderophore,
                MoleculeType.AminoMixGluFamily, MoleculeType.AminoMixAspFamily, MoleculeType.AminoMixSerGly
            }

            ' 遍历所有格子进行扩散
            For x As Integer = 0 To env.Width - 1
                For y As Integer = 0 To env.Height - 1
                    For z As Integer = 0 To env.Depth - 1
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

                                    Dim diff = voxel.ExternalMolecules(mol) - neighbor.ExternalMolecules(mol)
                                    If Math.Abs(diff) > 0 Then
                                        ' 扩散量 = 浓度差 × 扩散系数 × 生物膜因子
                                        Dim transfer = CInt(Math.Sign(diff) *
                                                     Math.Min(Math.Abs(diff) * 0.15 * diffusionFactor,
                                                              rng.NextInteger(1, 6)))
                                        If transfer <> 0 Then
                                            voxel.ExternalMolecules(mol) -= transfer
                                            neighbor.ExternalMolecules(mol) += transfer

                                            ' 确保不为负
                                            If voxel.ExternalMolecules(mol) < 0 Then
                                                neighbor.ExternalMolecules(mol) += voxel.ExternalMolecules(mol)
                                                voxel.ExternalMolecules(mol) = 0
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
