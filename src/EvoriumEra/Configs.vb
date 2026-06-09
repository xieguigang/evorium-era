Public Class Configs

    Public Property gridW As Integer = 100
    Public Property gridH As Integer = 100
    Public Property gridD As Integer = 100
    Public Property SnapshotInterval As Integer = 1

    Public Property InitCellNumbers As Integer = 50
    Public Property MaxVoxelActions As Integer = 5
    Public Property MaxCellActions As Integer = 10

    Public Property MaxCellContentCapacity As Integer = 10000

    Public Shared Function [Default]() As Configs
        Return New Configs
    End Function

End Class
