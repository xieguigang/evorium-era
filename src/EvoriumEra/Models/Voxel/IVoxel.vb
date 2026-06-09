Imports Microsoft.VisualBasic.Imaging

Public Interface IVoxel

    Property Position As SpatialIndex3D
    Property Molecules As Dictionary(Of MoleculeType, Integer)

End Interface
