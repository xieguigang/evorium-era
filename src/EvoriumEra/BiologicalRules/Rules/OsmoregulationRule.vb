Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports rng = Microsoft.VisualBasic.Math.RandomExtensions

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v3.0 核心] 渗透压调节规则
    ''' 
    ''' 离子强度影响细胞渗透压的机制：
    ''' 
    ''' 1. 高渗环境（环境离子强度 > 胞内）：
    '''    - 水从细胞内流向环境（细胞收缩）
    '''    - 胞内离子和小分子浓缩
    '''    - 细胞需要主动积累离子或合成相容溶质来恢复渗透平衡
    '''    - 高渗压力消耗额外ATP
    ''' 
    ''' 2. 低渗环境（环境离子强度 &lt; 胞内）：
    '''    - 水从环境流入细胞（细胞膨胀）
    '''    - 胞内分子稀释
    '''    - 有细胞壁的细胞可以抵抗膨胀压力
    '''    - 无细胞壁的细胞可能裂解
    '''    - 细胞需要排出离子来恢复渗透平衡
    ''' 
    ''' 3. 渗透压调节策略：
    '''    - Osmoregulation基因：主动调节Na+/K+泵，维持胞内离子平衡
    '''    - CompatibleSoluteSynthesis基因：合成甜菜碱/肌醇等相容溶质
    '''      相容溶质不干扰正常代谢，但能增加胞内渗透压
    '''    - 细胞壁提供低渗保护
    ''' </summary>
    Public Class OsmoregulationRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(GeneOntology.Osmoregulation, GeneOntology.CompatibleSoluteSynthesis)
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
            Dim externalIon = voxel.ExternalIonStrength
            Dim internalIon = cell.InternalIonStrength
            Dim isotonic = 300.0 ' 等渗参考值

            ' ===== 渗透压失衡检测 =====
            Dim osmoticDiff = externalIon - internalIon ' 正值=高渗，负值=低渗

            ' ===== 高渗响应 =====
            If osmoticDiff > 50 Then ' 显著高渗
                ' 水从细胞内流失
                Dim waterLoss = CInt(cell.GetMoleculeAmount(MoleculeType.Water) * 0.05)
                If waterLoss > 0 Then
                    cell.AddMoleculeInternal(MoleculeType.Water, -waterLoss)
                    voxel.ExternalMolecules(MoleculeType.Water).Quantity = voxel.GetMoleculeAmount(MoleculeType.Water) + waterLoss
                End If

                ' 胞内分子浓缩效应：少量ATP因浓缩而"获得"（模拟底物浓度升高）
                ' 但高渗压力本身消耗ATP
                cell.ATP = Math.Max(0, cell.ATP - 2)

                ' ===== 主动渗透压调节 =====
                If cell.HasFunction(GeneOntology.Osmoregulation) Then
                    ' Na+/K+泵：排出Na+，吸收K+（消耗ATP）
                    If cell.ATP >= 3 Then
                        cell.ATP -= 3
                        ' 排出Na+到环境
                        Dim naOut = Math.Min(cell.GetMoleculeAmount(MoleculeType.SodiumIon), 5)
                        If naOut > 0 Then
                            cell.AddMoleculeInternal(MoleculeType.SodiumIon, -naOut)
                            voxel.ExternalMolecules(MoleculeType.SodiumIon).Quantity = voxel.GetMoleculeAmount(MoleculeType.SodiumIon) + naOut
                        End If
                        ' 吸收K+从环境
                        Dim kIn = Math.Min(voxel.GetMoleculeAmount(MoleculeType.PotassiumIon), 3)
                        If kIn > 0 Then
                            cell.AddMoleculeInternal(MoleculeType.PotassiumIon, kIn)
                            voxel.ExternalMolecules(MoleculeType.PotassiumIon).Quantity = voxel.GetMoleculeAmount(MoleculeType.PotassiumIon) - kIn
                        End If
                    End If
                End If

                ' ===== 相容溶质合成 =====
                If cell.HasFunction(GeneOntology.CompatibleSoluteSynthesis) Then
                    Dim carbon = cell.GetMoleculeAmount(MoleculeType.CarbonSource)
                    Dim nitrogen = cell.GetMoleculeAmount(MoleculeType.NitrogenSource)
                    If carbon >= 3 AndAlso nitrogen >= 1 AndAlso cell.ATP >= 2 Then
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, -3)
                        cell.AddMoleculeInternal(MoleculeType.NitrogenSource, -1)
                        cell.ATP -= 2
                        ' 合成相容溶质，增加胞内渗透压但不干扰代谢
                        cell.AddMoleculeInternal(MoleculeType.CompatibleSolute, 5)
                    End If
                End If
            End If

            ' ===== 低渗响应 =====
            If osmoticDiff < -50 Then ' 显著低渗
                ' 水流入细胞
                Dim envWater = voxel.GetMoleculeAmount(MoleculeType.Water)
                Dim waterGain = CInt(envWater * 0.03)
                If waterGain > 0 Then
                    cell.AddMoleculeInternal(MoleculeType.Water, waterGain)
                    voxel.ExternalMolecules(MoleculeType.Water).Quantity = Math.Max(0, envWater - waterGain)
                End If

                ' 无细胞壁的细胞可能裂解
                If Not cell.HasCellWall Then
                    Dim lysisPressure = Math.Abs(osmoticDiff) / 500.0
                    If rng.NextDouble() < lysisPressure Then
                        ' 细胞因低渗裂解
                        Call env.LyseCell(cell, reason:="hypotonic_cell_lysis")
                    End If
                End If

                ' 主动排出离子降低胞内渗透压
                If cell.HasFunction(GeneOntology.Osmoregulation) Then
                    If cell.ATP >= 2 Then
                        cell.ATP -= 2
                        ' 排出K+
                        Dim kOut = Math.Min(cell.GetMoleculeAmount(MoleculeType.PotassiumIon), 3)
                        If kOut > 0 Then
                            cell.AddMoleculeInternal(MoleculeType.PotassiumIon, -kOut)
                            voxel.ExternalMolecules(MoleculeType.PotassiumIon).Quantity = voxel.GetMoleculeAmount(MoleculeType.PotassiumIon) + kOut
                        End If
                    End If
                End If
            End If

            ' ===== 相容溶质降解（渗透压恢复后回收） =====
            Dim compatibleSolute = cell.GetMoleculeAmount(MoleculeType.CompatibleSolute)
            If compatibleSolute > 0 AndAlso Math.Abs(osmoticDiff) < 30 Then
                ' 渗透压接近平衡时，降解相容溶质回收碳氮
                Dim degrade = Math.Min(compatibleSolute, 2)
                cell.AddMoleculeInternal(MoleculeType.CompatibleSolute, -degrade)
                cell.AddMoleculeInternal(MoleculeType.CarbonSource, degrade)
            End If
        End Sub
    End Class
End Namespace
