
Imports Microsoft.VisualBasic.Imaging

Public Class Environment3D

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

    ' ===== 构造函数 =====
    Public Sub New(w As Integer, h As Integer, d As Integer)
        Dimensions = (w, h, d)
        Grid = New Voxel(w - 1, h - 1, d - 1) {}

        For X As Integer = 0 To w - 1
            For Y As Integer = 0 To h - 1
                For z As Integer = 0 To d - 1
                    Grid(X, Y, z) = New Voxel(X, Y, z)
                Next
            Next
        Next
    End Sub

    ' ===== 邻居查找（6邻域）=====
    ''' <summary>
    ''' 获取指定体素的6个相邻体素（上下左右前后）
    ''' </summary>
    Public Function GetNeighbors(v As Voxel) As List(Of Voxel)
        Dim neighbors = New List(Of Voxel)()

        ' 六个方向：±X, ±Y, ±Z
        Dim directions = New List(Of (Integer, Integer, Integer)) From {
            (1, 0, 0), (-1, 0, 0),
            (0, 1, 0), (0, -1, 0),
            (0, 0, 1), (0, 0, -1)
        }

        For Each dir As (Integer, Integer, Integer) In directions
            Dim pos As SpatialIndex3D = v.Position
            Dim nx = pos.X + dir.Item1
            Dim ny = pos.Y + dir.Item2
            Dim nz = pos.Z + dir.Item3

            If IsValidCoordinate(nx, ny, nz) Then
                neighbors.Add(Grid(nx, ny, nz))
            End If
        Next

        Return neighbors
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
    Public Function AllCells() As IEnumerable(Of Cell)
        Return AllVoxels() _
            .Where(Function(v) v.Occupant IsNot Nothing AndAlso v.Occupant.IsAlive) _
            .Select(Function(v) v.Occupant)
    End Function

    ' ===== 随机空体素 =====
    ''' <summary>
    ''' 随机返回一个没有细胞占据的体素
    ''' </summary>
    Public Function GetRandomEmptyVoxel(rng As Random) As Voxel
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
    Private Function IsValidCoordinate(x As Integer, y As Integer, z As Integer) As Boolean
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