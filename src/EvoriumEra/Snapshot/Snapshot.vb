Imports Microsoft.VisualBasic.Imaging

Namespace Data

    ''' <summary>
    ''' A data snapshot of one time frame.
    ''' </summary>
    Public Class Snapshot
        ' 元数据
        Public Property Iteration As Long
        Public Property Timestamp As DateTime
        Public Property SimulationTime As TimeSpan

        ' 环境快照
        Public Property EnvironmentDimensions As SpatialIndex3D

        ' 统计汇总
        Public Property TotalLivingCells As Integer
        Public Property TotalDeadCells As Integer
        Public Property TotalMoleculesInSystem As Long
        Public Property AverageATPCells As Double

        ' 快速访问索引
        Private _voxelIndex As Dictionary(Of (Integer, Integer, Integer), VoxelSnapshot)

        Public Function GetVoxel(x As Integer, y As Integer, z As Integer) As VoxelSnapshot
            'If _voxelIndex Is Nothing Then
            '    _voxelIndex = New Dictionary(Of (Integer, Integer, Integer), VoxelSnapshot)

            '    For Each v As VoxelSnapshot In Voxels
            '        _voxelIndex((v.X, v.Y, v.Z)) = v
            '    Next
            'End If

            'Dim key = (x, y, z)
            'If _voxelIndex.ContainsKey(key) Then
            '    Return _voxelIndex(key)
            'End If
            'Return Nothing
        End Function
    End Class

End Namespace