Imports EvoriumEra.BiologicalRules.Rules
Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules

    ''' <summary>
    ''' Execute the biologicaal rules
    ''' </summary>
    Public Class RuleScheduler

        Public Property Rules As IBiochemicalRule()

        ReadOnly _functionMap As New Dictionary(Of GeneOntology, List(Of IBiochemicalRule))

        Public Sub New()
            ' 按顺序添加所有规则
            Rules = {
                New EnergyMetabolismRule(),
                New GeneExpressionRule(),
                New ReplicationAndDivisionRule(),
                New TransportRule(),
                New SynthesisAndDegradationRule(),
                New EnvironmentalResponseRule(),
                New MotionAndHGTRule(),
                New MutationRule(),
                New QuorumSensingAndBiofilmRule(),
                New DiffusionRule()
            }

            Call BuildFunctionMap()
        End Sub

        ''' <summary>
        ''' 轮盘赌选择（简化：按顺序执行，实际应按蛋白质浓度加权）
        ''' </summary>
        ''' <param name="cell"></param>
        ''' <param name="env"></param>
        Public Sub ApplyAll(cell As Cell, env As Environment3D)
            For Each rule As IBiochemicalRule In Rules
                If cell.IsAlive Then
                    Call rule.Execute(cell, env)
                End If
            Next
        End Sub

        Private Sub BuildFunctionMap()
            For Each rule As IBiochemicalRule In _Rules
                For Each f As GeneOntology In rule.SupportedFunctions
                    If Not _functionMap.ContainsKey(f) Then
                        _functionMap(f) = New List(Of IBiochemicalRule)
                    End If

                    Call _functionMap(f).Add(rule)
                Next
            Next
        End Sub

        ''' <summary>
        ''' 执行指定的基因功能
        ''' </summary>
        Public Sub ExecuteFunction(func As GeneOntology, cell As Cell, env As Environment3D)
            If _functionMap.ContainsKey(func) Then
                For Each rule As IBiochemicalRule In _functionMap(func)
                    rule.Execute(cell, env)
                Next
            End If
        End Sub
    End Class
End Namespace