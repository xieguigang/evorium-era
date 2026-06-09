Imports Microsoft.VisualBasic.Imaging

Public Class Voxel : Implements IVoxel
    Public Property Position As SpatialIndex3D Implements IVoxel.Position
    Public Property ExternalMolecules As New Dictionary(Of MoleculeType, Integer) Implements IVoxel.Molecules
    Public Property Occupant As Cell = Nothing
    Public Property HasBiofilm As Boolean = False
End Class