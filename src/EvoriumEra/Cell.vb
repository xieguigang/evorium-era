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
    Public Property Grid As Voxel(,,)
    Public Property Dimensions As (W As Integer, H As Integer, D As Integer)

    Public Sub New(w As Integer, h As Integer, d As Integer)
        Dimensions = (w, h, d)
        Grid = New Voxel(w - 1, h - 1, d - 1) {}
        For x = 0 To w - 1
            For y = 0 To h - 1
                For z = 0 To d - 1
                    Grid(x, y, z) = New Voxel With {.X = x, .Y = y, .Z = z}
                Next
            Next
        Next
    End Sub

    Public Function GetNeighbors(v As Voxel) As List(Of Voxel)
        Dim neighbors = New List(Of Voxel)
        ' 实现6邻域或26邻域
        Return neighbors
    End Function
End Class