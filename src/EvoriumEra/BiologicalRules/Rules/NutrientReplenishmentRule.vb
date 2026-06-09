Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports rng = Microsoft.VisualBasic.Math.RandomExtensions

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v2.0 新增] 营养补充与环境梯度规则
    ''' 
    ''' 核心功能：
    ''' 1. 氧气梯度：表层富氧，深层缺氧，创造好氧/厌氧生态位
    ''' 2. 营养热点：特定位置持续补充碳源和氮源
    ''' 3. 全局营养补充：每迭代向环境补充基础营养
    ''' 
    ''' 这些机制确保环境不会完全耗尽，维持群落的持续演化。
    ''' 氧气梯度是好氧/厌氧分层和交叉喂养链形成的前提。
    ''' </summary>
    Public Class NutrientReplenishmentRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New() ' 全局规则
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            ' 细胞级别不执行，由ExecuteEnvironment处理
        End Sub

        ''' <summary>
        ''' 环境级别的营养补充
        ''' </summary>
        Public Sub ExecuteEnvironment(env As NaturalEnvironment, config As Configs)
            ' 1. 氧气梯度补充
            ReplenishOxygenGradient(env, config)

            ' 2. 营养热点补充
            ReplenishNutrientHotspots(env, config)

            ' 3. 全局基础营养补充
            ReplenishGlobalNutrients(env, config)
        End Sub

        Private Sub ReplenishOxygenGradient(env As NaturalEnvironment, config As Configs)
            ' 氧气浓度随Z轴（深度）递减
            ' 表层（z=0）最富氧，底层（z=max）最缺氧
            For x As Integer = 0 To env.Width - 1
                For y As Integer = 0 To env.Height - 1
                    For z As Integer = 0 To env.Depth - 1
                        Dim voxel = env.Grid(x, y, z)

                        ' 计算该深度的目标氧气浓度
                        Dim depthFraction = CDbl(z) / Math.Max(1, env.Depth - 1)
                        Dim targetOxygen = CInt(config.SurfaceOxygenLevel * (1.0 - depthFraction * config.OxygenGradientDecay))

                        ' 向目标浓度缓慢趋近
                        Dim currentOxygen = voxel.GetMoleculeAmount(MoleculeType.Oxygen)
                        Dim delta = CInt((targetOxygen - currentOxygen) * 0.1) ' 每次趋近10%

                        If delta <> 0 Then
                            If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.Oxygen) Then
                                voxel.ExternalMolecules(MoleculeType.Oxygen) = 0
                            End If
                            voxel.ExternalMolecules(MoleculeType.Oxygen) = Math.Max(0, currentOxygen + delta)
                        End If
                    Next
                Next
            Next
        End Sub

        Private Sub ReplenishNutrientHotspots(env As NaturalEnvironment, config As Configs)
            ' 使用确定性种子生成固定位置的营养热点
            ' 热点位置基于网格尺寸均匀分布
            Dim hotspotPositions = GenerateHotspotPositions(env, config.NutrientHotspotCount)

            For Each pos In hotspotPositions
                If env.IsValidCoordinate(pos.X, pos.Y, pos.Z) Then
                    Dim voxel = env.Grid(pos.X, pos.Y, pos.Z)

                    ' 补充碳源
                    If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.CarbonSource) Then
                        voxel.ExternalMolecules(MoleculeType.CarbonSource) = 0
                    End If
                    voxel.ExternalMolecules(MoleculeType.CarbonSource) += config.NutrientHotspotStrength

                    ' 补充氮源
                    If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.NitrogenSource) Then
                        voxel.ExternalMolecules(MoleculeType.NitrogenSource) = 0
                    End If
                    voxel.ExternalMolecules(MoleculeType.NitrogenSource) += config.NutrientHotspotStrength \ 2

                    ' 补充葡萄糖
                    If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.Glucose) Then
                        voxel.ExternalMolecules(MoleculeType.Glucose) = 0
                    End If
                    voxel.ExternalMolecules(MoleculeType.Glucose) += config.NutrientHotspotStrength \ 3
                End If
            Next
        End Sub

        Private Sub ReplenishGlobalNutrients(env As NaturalEnvironment, config As Configs)
            ' 每迭代向随机位置补充少量基础营养
            For i As Integer = 0 To 9
                Dim x = rng.Next(env.Width)
                Dim y = rng.Next(env.Height)
                Dim z = rng.Next(env.Depth)
                Dim voxel = env.Grid(x, y, z)

                If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.CarbonSource) Then
                    voxel.ExternalMolecules(MoleculeType.CarbonSource) = 0
                End If
                If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.NitrogenSource) Then
                    voxel.ExternalMolecules(MoleculeType.NitrogenSource) = 0
                End If
                If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.Water) Then
                    voxel.ExternalMolecules(MoleculeType.Water) = 0
                End If

                voxel.ExternalMolecules(MoleculeType.CarbonSource) += config.NutrientReplenishmentCarbon \ 10
                voxel.ExternalMolecules(MoleculeType.NitrogenSource) += config.NutrientReplenishmentNitrogen \ 10
                voxel.ExternalMolecules(MoleculeType.Water) += 5
            Next
        End Sub

        Private Function GenerateHotspotPositions(env As NaturalEnvironment, count As Integer) As List(Of SpatialIndex3D)
            Dim positions = New List(Of SpatialIndex3D)()

            ' 均匀分布热点
            Dim gridSize = Math.Max(env.Width, env.Height)
            Dim spacing = gridSize / Math.Sqrt(count)

            Dim idx = 0
            For x As Double = spacing / 2 To env.Width - 1 Step spacing
                For y As Double = spacing / 2 To env.Height - 1 Step spacing
                    If idx >= count Then Exit For
                    ' 热点主要在表层和中层
                    Dim z = rng.Next(CInt(env.Depth * 0.7))
                    positions.Add(New SpatialIndex3D(CInt(x), CInt(y), z))
                    idx += 1
                Next
            Next

            Return positions
        End Function
    End Class
End Namespace
