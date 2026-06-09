Imports EvoriumEra.BiologicalRules.Rules
Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules

    Public Class RuleScheduler

        Public Property Rules As IBiochemicalRule()

        ReadOnly _functionMap As New Dictionary(Of GeneOntology, List(Of IBiochemicalRule))

        Public Sub New()
            Rules = {
                New EnergyMetabolismRule(),
                New MetabolicPathwayRule(),          ' [v2.0] 核心代谢链
                New GeneExpressionRule(),             ' [v2.0] 改进：真正转录基因→蛋白质
                New ReplicationAndDivisionRule(),     ' [v2.0] 改进：增加分裂条件
                New TransportRule(),                  ' [v2.0] 改进：增加溢流分泌
                New SynthesisAndDegradationRule(),
                New EnvironmentalResponseRule(),
                New MotionAndHGTRule(),
                New MutationRule(),                   ' [v2.0] 改进：增加基因重复/缺失
                New QuorumSensingAndBiofilmRule(),
                New DiffusionRule(),                  ' [v2.0] 改进：代谢中间产物可扩散
                New GenomeMaintenanceRule(),          ' [v2.0] 新增：基因组维护成本
                New NutrientReplenishmentRule(),      ' [v2.0] 新增：环境营养补充
                New CellLysisRule()                   ' [v2.0] 新增：细胞裂解释放
            }

            BuildFunctionMap()
        End Sub

        Private Sub BuildFunctionMap()
            For Each rule As IBiochemicalRule In Rules
                For Each f As GeneOntology In rule.SupportedFunctions
                    If Not _functionMap.ContainsKey(f) Then
                        _functionMap(f) = New List(Of IBiochemicalRule)
                    End If
                    _functionMap(f).Add(rule)
                Next
            Next
        End Sub

        Public Sub ExecuteFunction(func As GeneOntology, cell As Cell, env As NaturalEnvironment)
            If _functionMap.ContainsKey(func) Then
                For Each rule As IBiochemicalRule In _functionMap(func)
                    rule.Execute(cell, env)
                Next
            End If
        End Sub

        ''' <summary>
        ''' [v2.0] 执行所有非基因功能驱动的全局规则
        ''' </summary>
        Public Sub ExecuteGlobalRules(cell As Cell, env As NaturalEnvironment)
            For Each rule As IBiochemicalRule In Rules
                ' 全局规则是 SupportedFunctions 为空的规则
                If rule.SupportedFunctions Is Nothing OrElse rule.SupportedFunctions.Length = 0 Then
                    rule.Execute(cell, env)
                End If
            Next
        End Sub

        ''' <summary>
        ''' [v2.0] 执行环境级别的规则（不针对特定细胞）
        ''' </summary>
        Public Sub ExecuteEnvironmentRules(env As NaturalEnvironment, config As Configs)
            ' 营养补充和扩散在环境级别执行
            For Each rule As IBiochemicalRule In Rules
                If TypeOf rule Is NutrientReplenishmentRule Then
                    DirectCast(rule, NutrientReplenishmentRule).ExecuteEnvironment(env, config)
                ElseIf TypeOf rule Is DiffusionRule Then
                    DirectCast(rule, DiffusionRule).ExecuteEnvironment(env)
                End If
            Next
        End Sub
    End Class
End Namespace
