Imports Microsoft.VisualBasic.Imaging

Public Class Voxel : Implements IVoxel

    Public Property Position As SpatialIndex3D Implements IVoxel.Position
    Public Property ExternalMolecules As New Dictionary(Of MoleculeType, Integer) Implements IVoxel.Molecules
    Public Property Occupant As Cell = Nothing
    Public Property HasBiofilm As Boolean = False

    Sub New()
    End Sub

    Sub New(x As Integer, y As Integer, z As Integer)
        Position = New SpatialIndex3D(x, y, z)
    End Sub

    Public Overrides Function ToString() As String
        Return Position.ToString
    End Function
End Class