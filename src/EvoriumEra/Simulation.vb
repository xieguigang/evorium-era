Public Class Simulation
    Public Property Env As Environment3D
    Public Property Scheduler As RuleScheduler
    Public Property RNG As New Random()
    Public Property CurrentIteration As Integer = 0
    Public Property SnapshotRoot As String = "Snapshots"

    Public Sub StepOnce()
        CurrentIteration += 1
        ' 随机打乱格子顺序
        Dim voxels = ShuffleVoxels()
        For Each v In voxels
            If v.Occupant IsNot Nothing AndAlso v.Occupant.IsAlive Then
                Scheduler.ApplyAll(v.Occupant, Env, RNG)
                CheckCellDeath(v.Occupant)
            End If
            DiffuseMolecules(v)
        Next
        SaveSnapshot()
    End Sub

    Private Sub DiffuseMolecules(v As Voxel)
        ' 基于浓度差的扩散逻辑
    End Sub

    Private Sub CheckCellDeath(cell As Cell)
        If cell.ATP <= 0 Then
            cell.ConsecutiveNoATP += 1
            If cell.ConsecutiveNoATP >= 5 Then
                cell.IsAlive = False
                LyseCell(cell)
            End If
        Else
            cell.ConsecutiveNoATP = 0
        End If
    End Sub

    Private Sub LyseCell(cell As Cell)
        ' 释放内容物到当前格子
    End Sub
End Class