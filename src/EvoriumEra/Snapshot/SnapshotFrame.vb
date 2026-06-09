
Namespace Data

    Public Class SnapshotFrame

        Public Property metadata As Snapshot
        Public Property voxels As VoxelSnapshot()
        Public Property cells As CellSnapshot()

        ' 快速访问索引
        Private _voxelIndex As Dictionary(Of (Integer, Integer, Integer), VoxelSnapshot)

        Public Function GetVoxel(x As Integer, y As Integer, z As Integer) As VoxelSnapshot
            If _voxelIndex Is Nothing Then
                _voxelIndex = New Dictionary(Of (Integer, Integer, Integer), VoxelSnapshot)

                For Each v As VoxelSnapshot In voxels
                    _voxelIndex((v.X, v.Y, v.Z)) = v
                Next
            End If

            Dim key = (x, y, z)
            If _voxelIndex.ContainsKey(key) Then
                Return _voxelIndex(key)
            End If
            Return Nothing
        End Function
    End Class
End Namespace