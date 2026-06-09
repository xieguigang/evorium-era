


Public Class RuleScheduler
    Public Property Rules As List(Of IBiochemicalRule) = New List(Of IBiochemicalRule)
    Dim _functionMap As Dictionary(Of GeneOntology, List(Of IBiochemicalRule))

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

        BuildFunctionMap()
    End Sub

    Public Sub ApplyAll(cell As Cell, env As Environment3D)
        ' 轮盘赌选择（简化：按顺序执行，实际应按蛋白质浓度加权）
        For Each rule In Rules
            If cell.IsAlive Then
                Try
                    rule.Execute(cell, env)
                Catch ex As Exception
                    ' 记录异常但继续执行
                End Try
            End If
        Next
    End Sub

    Private Sub BuildFunctionMap()
        _functionMap = New Dictionary(Of GeneOntology, List(Of IBiochemicalRule))

        For Each rule In _Rules
            For Each f In rule.SupportedFunctions
                If Not _functionMap.ContainsKey(f) Then
                    _functionMap(f) = New List(Of IBiochemicalRule)
                End If
                _functionMap(f).Add(rule)
            Next
        Next
    End Sub

    ''' <summary>
    ''' 执行指定的基因功能
    ''' </summary>
    Public Sub ExecuteFunction(func As GeneOntology, cell As Cell, env As Environment3D)
        If Not _functionMap.ContainsKey(func) Then Return

        For Each rule In _functionMap(func)
            rule.Execute(cell, env)
        Next
    End Sub
End Class
