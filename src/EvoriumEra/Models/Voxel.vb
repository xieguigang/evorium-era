Public Class Voxel
    Public Property X As Integer
    Public Property Y As Integer
    Public Property Z As Integer
    Public Property ExternalMolecules As Dictionary(Of MoleculeType, Integer) = New Dictionary(Of MoleculeType, Integer)
    Public Property Occupant As Cell = Nothing
    Public Property HasBiofilm As Boolean = False
End Class