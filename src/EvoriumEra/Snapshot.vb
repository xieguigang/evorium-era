Public Class Snapshot
    Public Property Iteration As Integer
    Public Property Timestamp As DateTime
    Public Property Cells As List(Of CellSnapshot)
    Public Property Voxels As List(Of VoxelSnapshot)
End Class

Public Class CellSnapshot
    Public Property ID As Guid
    Public Property Position As (Integer, Integer, Integer)
    Public Property Molecules As Dictionary(Of MoleculeType, Integer)
    Public Property ATP As Integer
End Class