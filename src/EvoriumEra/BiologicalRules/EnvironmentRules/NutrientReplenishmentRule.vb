Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports Microsoft.VisualBasic.Imaging
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
    ''' 
    ''' [v3.0 改进] 营养补充与环境梯度规则
    ''' 
    ''' v3.0改进：
    ''' 1. 增加离子补充（Na+, K+, Cl-, phosphate, sulfate, Fe2+/Fe3+）
    ''' 2. 增加温度梯度初始化
    ''' </summary>
    Public Class NutrientReplenishmentRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New()
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            ' 细胞级别不执行，由ExecuteEnvironment处理
        End Sub

        ''' <summary>
        ''' 环境级别的营养补充
        ''' </summary>
        Public Sub ExecuteEnvironment(env As NaturalEnvironment)
            ' 1. 氧气梯度
            ReplenishOxygenGradient(env)

            ' 2. 营养热点
            ReplenishNutrientHotspots(env)

            ' 3. 全局营养补充
            ReplenishGlobalNutrients(env)

            ' 4. [v3.0] 离子补充
            ReplenishIons(env)
        End Sub

        Private Sub ReplenishOxygenGradient(env As NaturalEnvironment)
            Dim dims = env.Dimensions
            Dim config = env.configs

            ' 氧气浓度随Z轴（深度）递减
            ' 表层（z=0）最富氧，底层（z=max）最缺氧
            For x As Integer = 0 To dims.Width - 1
                For y As Integer = 0 To dims.Height - 1
                    For z As Integer = 0 To dims.Depth - 1
                        Dim voxel = env.Grid(x, y, z)

                        ' 计算该深度的目标氧气浓度
                        Dim depthFraction = CDbl(z) / Math.Max(1, dims.Depth - 1)
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

        Private Sub ReplenishNutrientHotspots(env As NaturalEnvironment)
            Dim config = env.configs
            Dim hotspots = GenerateHotspotPositions(env, config.NutrientHotspotCount)

            ' 使用确定性种子生成固定位置的营养热点
            ' 热点位置基于网格尺寸均匀分布

            For Each pos In hotspots
                If env.IsValidCoordinate(pos.X, pos.Y, pos.Z) Then
                    Dim voxel = env.Grid(pos.X, pos.Y, pos.Z)

                    If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.CarbonSource) Then
                        voxel.ExternalMolecules(MoleculeType.CarbonSource) = 0
                    End If
                    If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.NitrogenSource) Then
                        voxel.ExternalMolecules(MoleculeType.NitrogenSource) = 0
                    End If

                    ' 补充碳源/补充氮源
                    voxel.ExternalMolecules(MoleculeType.CarbonSource) += config.NutrientHotspotStrength
                    voxel.ExternalMolecules(MoleculeType.NitrogenSource) += config.NutrientHotspotStrength \ 2

                    ' [v3.0] 热点也补充glucose
                    If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.Glucose) Then
                        voxel.ExternalMolecules(MoleculeType.Glucose) = 0
                    End If
                    voxel.ExternalMolecules(MoleculeType.Glucose) += config.NutrientHotspotStrength \ 3
                End If
            Next
        End Sub

        Private Sub ReplenishGlobalNutrients(env As NaturalEnvironment)
            Dim dims = env.Dimensions
            Dim config = env.configs

            ' 每迭代向随机位置补充少量基础营养
            For i As Integer = 0 To 9
                Dim x = rng.Next(dims.Width)
                Dim y = rng.Next(dims.Height)
                Dim z = rng.Next(dims.Depth)
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

        ''' <summary>
        ''' [v3.0] 离子补充
        ''' </summary>
        Private Sub ReplenishIons(env As NaturalEnvironment)
            Dim dims = env.Dimensions
            Dim config = env.configs

            For X As Integer = 0 To dims.Width - 1 Step 5
                For Y As Integer = 0 To dims.Height - 1 Step 5
                    For z As Integer = 0 To dims.Depth - 1 Step 5
                        Dim voxel = env.Grid(X, Y, z)

                        ' 确保基础离子浓度
                        EnsureMolecule(voxel, MoleculeType.SodiumIon, config.InitialSaltIonLevel \ 5)
                        EnsureMolecule(voxel, MoleculeType.PotassiumIon, config.InitialSaltIonLevel \ 10)
                        EnsureMolecule(voxel, MoleculeType.ChlorideIon, config.InitialSaltIonLevel \ 5)
                        EnsureMolecule(voxel, MoleculeType.Phosphate, config.InitialPhosphateLevel \ 10)
                        EnsureMolecule(voxel, MoleculeType.Sulfate, config.InitialSulfateLevel \ 10)
                        EnsureMolecule(voxel, MoleculeType.IronII, config.InitialIronLevel \ 10)
                        EnsureMolecule(voxel, MoleculeType.MagnesiumIon, 5)
                        EnsureMolecule(voxel, MoleculeType.CalciumIon, 3)
                    Next
                Next
            Next
        End Sub

        Private Sub EnsureMolecule(voxel As Container.Voxel, type As MoleculeType, minAmount As Integer)
            If Not voxel.ExternalMolecules.ContainsKey(type) Then
                voxel.ExternalMolecules(type) = 0
            End If
            If voxel.ExternalMolecules(type) < minAmount Then
                voxel.ExternalMolecules(type) = minAmount
            End If
        End Sub

        Private Function GenerateHotspotPositions(env As NaturalEnvironment, count As Integer) As List(Of SpatialIndex3D)
            Dim positions = New List(Of SpatialIndex3D)()
            Dim dims = env.Dimensions
            Dim gridSize = Math.Max(dims.Width, dims.Height)
            Dim spacing = gridSize / Math.Sqrt(count)

            Dim idx = 0
            For x As Double = spacing / 2 To dims.Width - 1 Step spacing
                For y As Double = spacing / 2 To dims.Height - 1 Step spacing
                    If idx >= count Then Exit For
                    Dim z = rng.Next(CInt(dims.Depth * 0.7))
                    positions.Add(New SpatialIndex3D(CInt(x), CInt(y), z))
                    idx += 1
                Next
            Next

            Return positions
        End Function
    End Class
End Namespace
