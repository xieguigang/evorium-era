Public Class Cell : Implements IVoxel
    Public Property ID As Guid = Guid.NewGuid()
    Public Property ParentID As Guid? = Nothing
    Public Property Generation As Integer = 0
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

