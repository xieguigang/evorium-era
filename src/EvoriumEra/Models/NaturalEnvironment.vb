
Imports System.Runtime.CompilerServices
Imports EvoriumEra.Models.Container
Imports Microsoft.VisualBasic.Imaging
Imports rng = Microsoft.VisualBasic.Math.RandomExtensions

Namespace Models

    ''' <summary>
    ''' 自然环境：提供微生物群落的生长环境，包括营养源，模拟的空间位置等信息。在这个模拟程序中，自然环境被定义为一个被划分为有限数量的格子的三维空间，一个格子内只能够容纳一个细胞，但是可以容纳任意数量的分子对象。自然环境空间在这个模拟程序中可以为任意形状的
    ''' </summary>
    ''' <remarks>
    ''' Natural Environment: Provides a growth habitat for microbial communities, including nutrient sources and simulated spatial locations. 
    ''' In this simulation, the natural environment is defined as a three-dimensional space partitioned into a finite number of grids. 
    ''' A single grid can accommodate only one cell, but can hold an arbitrary number of molecular objects. The natural environment space 
    ''' in this simulation can take on any arbitrary shape.
    ''' </remarks>
    Public Class NaturalEnvironment

        ''' <summary>
        ''' ===== 网格数据 =====
        ''' </summary>
        ''' <returns></returns>
        Public Property Grid As Voxel(,,)
        Public Property Dimensions As (Width As Integer, Height As Integer, Depth As Integer)

        Default Public ReadOnly Property Voxel(ref As IVoxel) As Voxel
            Get
                Return _Grid(ref.Position.X, ref.Position.Y, ref.Position.Z)
            End Get
        End Property

        Default Public ReadOnly Property Voxel(x As Integer, y As Integer, z As Integer) As Voxel
            Get
                Return _Grid(x, y, z)
            End Get
        End Property

        Friend ReadOnly configs As Configs
        Friend moleculeUtils As MoleculeUtils

        ' ===== 构造函数 =====
        Public Sub New(config As Configs)
            Dim w As Integer = config.gridW
            Dim h As Integer = config.gridH
            Dim d As Integer = config.gridD

            Dimensions = (w, h, d)
            Grid = New Voxel(w - 1, h - 1, d - 1) {}
            configs = config
            moleculeUtils = New MoleculeUtils(configs, Me)

            For X As Integer = 0 To w - 1
                For Y As Integer = 0 To h - 1
                    For z As Integer = 0 To d - 1
                        Grid(X, Y, z) = New Voxel(X, Y, z)
                    Next
                Next
            Next
        End Sub

        ''' <summary>
        ''' 向容器中添加或移除分子
        ''' </summary>
        ''' <param name="container">可以是Cell或Voxel</param>
        ''' <param name="moleculeType">分子类型</param>
        ''' <param name="amount">正数增加，负数减少</param>
        ''' 
        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Sub AddMolecule(container As IVoxel, moleculeType As MoleculeType, amount As Integer)
            Call moleculeUtils.AddMolecule(container, moleculeType, amount)
        End Sub

        ''' <summary>
        ''' 杀死细胞，并将细胞内所有物质释放到当前格子
        ''' </summary>
        ''' <param name="cell"></param>
        ''' 
        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Sub LyseCell(cell As Cell)
            Call moleculeUtils.LyseCell(cell)
        End Sub

        ' 六个方向：±X, ±Y, ±Z
        Shared ReadOnly directions As (Integer, Integer, Integer)() = {
            (1, 0, 0), (-1, 0, 0),
            (0, 1, 0), (0, -1, 0),
            (0, 0, 1), (0, 0, -1)
        }

        ' ===== 邻居查找（6邻域）=====
        ''' <summary>
        ''' 获取指定体素的6个相邻体素（上下左右前后）
        ''' </summary>
        Public Iterator Function GetNeighbors(v As Voxel) As IEnumerable(Of Voxel)
            For Each dir As (Integer, Integer, Integer) In directions
                Dim pos As SpatialIndex3D = v.Position
                Dim nx = pos.X + dir.Item1
                Dim ny = pos.Y + dir.Item2
                Dim nz = pos.Z + dir.Item3

                If IsValidCoordinate(nx, ny, nz) Then
                    Yield _Grid(nx, ny, nz)
                End If
            Next
        End Function

        ' ===== 所有体素枚举 =====
        ''' <summary>
        ''' 返回环境中所有体素的扁平枚举
        ''' </summary>
        Public Iterator Function AllVoxels() As IEnumerable(Of Voxel)
            For x As Integer = 0 To Dimensions.Width - 1
                For Y As Integer = 0 To Dimensions.Height - 1
                    For z As Integer = 0 To Dimensions.Depth - 1
                        Yield Grid(x, Y, z)
                    Next
                Next
            Next
        End Function

        ' ===== 所有活细胞 =====
        ''' <summary>
        ''' 返回环境中所有存活的细胞
        ''' </summary>
        ''' 
        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Function AllCells() As IEnumerable(Of Cell)
            Return From v As Voxel
                   In AllVoxels()
                   Where v.Occupant IsNot Nothing AndAlso v.Occupant.IsAlive
                   Select v.Occupant
        End Function

        ' ===== 随机空体素 =====
        ''' <summary>
        ''' 随机返回一个没有细胞占据的体素
        ''' </summary>
        Public Function GetRandomEmptyVoxel() As Voxel
            Dim emptyVoxels = AllVoxels() _
                .Where(Function(v) v.Occupant Is Nothing) _
                .ToList()

            If emptyVoxels.Count = 0 Then Return Nothing

            Return emptyVoxels(rng.Next(emptyVoxels.Count))
        End Function

        ''' <summary>
        ''' ===== 坐标有效性检查 =====
        ''' </summary>
        ''' <param name="x"></param>
        ''' <param name="y"></param>
        ''' <param name="z"></param>
        ''' <returns></returns>
        Public Function IsValidCoordinate(x As Integer, y As Integer, z As Integer) As Boolean
            Return x >= 0 AndAlso x < Dimensions.Width AndAlso
               y >= 0 AndAlso y < Dimensions.Height AndAlso
               z >= 0 AndAlso z < Dimensions.Depth
        End Function

        ''' <summary>
        ''' ===== 距离计算（可选，用于高级规则）=====
        ''' </summary>
        ''' <param name="v1"></param>
        ''' <param name="v2"></param>
        ''' <returns></returns>
        Public Function ManhattanDistance(v1 As Voxel, v2 As Voxel) As Integer
            Dim d = v1.Position - v2.Position

            Return Math.Abs(d.X) +
               Math.Abs(d.Y) +
               Math.Abs(d.Z)
        End Function

        ''' <summary>
        ''' ===== 区域查询 =====
        ''' </summary>
        ''' <param name="center"></param>
        ''' <param name="radius"></param>
        ''' <returns></returns>
        Public Iterator Function GetVoxelsInRadius(center As Voxel, radius As Integer) As IEnumerable(Of Voxel)
            Dim centerPos As SpatialIndex3D = center.Position

            For X As Integer = centerPos.X - radius To centerPos.X + radius
                For Y As Integer = centerPos.Y - radius To centerPos.Y + radius
                    For z As Integer = centerPos.Z - radius To centerPos.Z + radius
                        If IsValidCoordinate(X, Y, z) Then
                            Dim v = Grid(X, Y, z)
                            If ManhattanDistance(center, v) <= radius Then
                                Yield v
                            End If
                        End If
                    Next
                Next
            Next
        End Function
    End Class
End Namespace