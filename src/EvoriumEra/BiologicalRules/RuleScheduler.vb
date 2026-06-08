Public Interface IBiochemicalRule
    Sub Execute(cell As Cell, env As Environment3D, rng As Random)
End Interface



Public Class RuleScheduler
    Public Property Rules As List(Of IBiochemicalRule) = New List(Of IBiochemicalRule)

    Public Sub New()
        ' 按顺序添加所有规则
        Rules.Add(New EnergyMetabolismRule())
        Rules.Add(New GeneExpressionRule())
        Rules.Add(New ReplicationAndDivisionRule())
        Rules.Add(New TransportRule())
        Rules.Add(New SynthesisAndDegradationRule())
        Rules.Add(New EnvironmentalResponseRule())
        Rules.Add(New MotionAndHGTRule())
        Rules.Add(New MutationRule())
        Rules.Add(New QuorumSensingAndBiofilmRule())
        Rules.Add(New DiffusionRule())
    End Sub

    Public Sub ApplyAll(cell As Cell, env As Environment3D, rng As Random)
        ' 轮盘赌选择（简化：按顺序执行，实际应按蛋白质浓度加权）
        For Each rule In Rules
            If cell.IsAlive Then
                Try
                    rule.Execute(cell, env, rng)
                Catch ex As Exception
                    ' 记录异常但继续执行
                End Try
            End If
        Next
    End Sub
End Class
