Imports Microsoft.VisualBasic.Imaging

Namespace Models.Container

    Public Class Cell : Implements IVoxel

        Public Property ID As Guid = Guid.NewGuid()
        Public Property ParentID As Guid? = Nothing
        Public Property Generation As Integer = 0
        Public Property Position As SpatialIndex3D Implements IVoxel.Position
        Public Property Genome As Replicon
        Public Property Plasmids As List(Of Replicon) = New List(Of Replicon)
        Public Property InternalMolecules As New Dictionary(Of MoleculeType, Integer) Implements IVoxel.Molecules
        Public Property Proteins As New Dictionary(Of GeneOntology, Integer)
        Public Property HasCellWall As Boolean = False
        Public Property IsAlive As Boolean = True
        Public Property ATP As Integer = 100
        Public Property ConsecutiveNoATP As Integer = 0
        Public Property TotalMolecules As Integer = 0

    End Class

End Namespace