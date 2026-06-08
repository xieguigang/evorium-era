Public Class Cell
    Public Property ID As Guid = Guid.NewGuid()
    Public Property Position As (X As Integer, Y As Integer, Z As Integer)
    Public Property Genome As Replicon
    Public Property Plasmids As List(Of Replicon) = New List(Of Replicon)
    Public Property InternalMolecules As Dictionary(Of MoleculeType, Integer) = New Dictionary(Of MoleculeType, Integer)
    Public Property Proteins As Dictionary(Of GeneFunction, Integer) = New Dictionary(Of GeneFunction, Integer)
    Public Property HasCellWall As Boolean = False
    Public Property IsAlive As Boolean = True
    Public Property ATP As Integer = 100
    Public Property ConsecutiveNoATP As Integer = 0
    Public Property TotalMolecules As Integer = 0
    Public Const MaxCapacity As Integer = 10000
End Class

Public Class Voxel
    Public Property X As Integer
    Public Property Y As Integer
    Public Property Z As Integer
    Public Property ExternalMolecules As Dictionary(Of MoleculeType, Integer) = New Dictionary(Of MoleculeType, Integer)
    Public Property Occupant As Cell = Nothing
    Public Property HasBiofilm As Boolean = False
End Class

Public Class Environment3D
    ' ===== 网格数据 =====
    Public Property Grid As Voxel(,,)
    Public Property Dimensions As (Width As Integer, Height As Integer, Depth As Integer)

    ' ===== 构造函数 =====
    Public Sub New(w As Integer, h As Integer, d As Integer)
        Dimensions = (w, h, d)
        Grid = New Voxel(w - 1, h - 1, d - 1) {}

        For x = 0 To w - 1
            For y = 0 To h - 1
                For z = 0 To d - 1
                    Grid(x, y, z) = New Voxel With {
                        .X = x,
                        .Y = y,
                        .Z = z
                    }
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
            Dim nx = v.X + dir.Item1
            Dim ny = v.Y + dir.Item2
            Dim nz = v.Z + dir.Item3

            If IsValidCoordinate(nx, ny, nz) Then
                neighbors.Add(Grid(nx, ny, nz))
            End If
        Next

        Return neighbors
    End Function

    Private Function WrapCoordinate(value As Integer, max As Integer) As Integer
        If value < 0 Then Return max - 1
        If value >= max Then Return 0
        Return value
    End Function

    ' ===== 所有体素枚举 =====
    ''' <summary>
    ''' 返回环境中所有体素的扁平枚举
    ''' </summary>
    Public Iterator Function AllVoxels() As IEnumerable(Of Voxel)
        For x = 0 To Dimensions.Width - 1
            For y = 0 To Dimensions.Height - 1
                For z = 0 To Dimensions.Depth - 1
                    Yield Grid(x, y, z)
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

    ' ===== 坐标有效性检查 =====
    Private Function IsValidCoordinate(x As Integer, y As Integer, z As Integer) As Boolean
        Return x >= 0 AndAlso x < Dimensions.Width AndAlso
               y >= 0 AndAlso y < Dimensions.Height AndAlso
               z >= 0 AndAlso z < Dimensions.Depth
    End Function

    ' ===== 距离计算（可选，用于高级规则）=====
    Public Function ManhattanDistance(v1 As Voxel, v2 As Voxel) As Integer
        Return Math.Abs(v1.X - v2.X) +
               Math.Abs(v1.Y - v2.Y) +
               Math.Abs(v1.Z - v2.Z)
    End Function

    ' ===== 区域查询 =====
    Public Function GetVoxelsInRadius(center As Voxel, radius As Integer) As List(Of Voxel)
        Dim result = New List(Of Voxel)()

        For x = center.X - radius To center.X + radius
            For y = center.Y - radius To center.Y + radius
                For z = center.Z - radius To center.Z + radius
                    If IsValidCoordinate(x, y, z) Then
                        Dim v = Grid(x, y, z)
                        If ManhattanDistance(center, v) <= radius Then
                            result.Add(v)
                        End If
                    End If
                Next
            Next
        Next

        Return result
    End Function
End Class