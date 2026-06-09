Imports Microsoft.VisualBasic.Imaging

Namespace Models.Container

    Public Class Voxel : Implements IVoxel

        Public Property Position As SpatialIndex3D Implements IVoxel.Position
        Public Property ExternalMolecules As New Dictionary(Of MoleculeType, Integer) Implements IVoxel.Molecules
        Public Property Occupant As Cell = Nothing
        Public Property HasBiofilm As Boolean = False

        ' [v2.0] 生物膜强度（0-100），影响物质交换阻断程度
        Public Property BiofilmStrength As Integer = 0

        Sub New()
        End Sub

        Sub New(x As Integer, y As Integer, z As Integer)
            Position = New SpatialIndex3D(x, y, z)
        End Sub

        ''' <summary>
        ''' [v2.0] 获取格子内指定分子的数量
        ''' </summary>
        Public Function GetMoleculeAmount(type As MoleculeType) As Integer
            If ExternalMolecules.ContainsKey(type) Then
                Return ExternalMolecules(type)
            Else
                Return 0
            End If
        End Function

        Public Overrides Function ToString() As String
            Return Position.ToString
        End Function
    End Class
End Namespace
