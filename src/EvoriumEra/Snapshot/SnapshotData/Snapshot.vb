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

    End Class

End Namespace