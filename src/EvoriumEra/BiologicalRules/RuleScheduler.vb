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
                New ExtendedMetabolicPathwayRule(),  ' [v3.0] 扩展代谢链
                New GeneExpressionRule(),            ' [v2.0] 改进：真正转录基因→蛋白质
                New ReplicationAndDivisionRule(),    ' [v2.0] 改进：增加分裂条件
                New TransportRule(),                 ' [v2.0] 改进：增加溢流分泌
                New ExtendedSynthesisRule(),         ' [v3.0] 扩展合成
                New EnvironmentalResponseRule(),
                New MotionAndHGTRule(),
                New MutationRule(),                  ' [v2.0] 改进：增加基因重复/缺失
                New QuorumSensingAndBiofilmRule(),
                New DiffusionRule(),                 ' [v2.0] 改进：代谢中间产物可扩散
                New NutrientReplenishmentRule(),     ' [v2.0] 新增：环境营养补充
                New GenomeMaintenanceRule(),         ' [v2.0] 新增：基因组维护成本
                New CellLysisRule(),                 ' [v2.0] 新增：细胞裂解释放
                New TemperatureRule(),               ' [v3.0] 温度系统
                New OsmoregulationRule(),            ' [v3.0] 渗透压系统
                New IonTransportRule()               ' [v3.0] 离子转运
            }

            ' BuildFunctionMap
            For Each rule As IBiochemicalRule In Rules
                For Each f As GeneOntology In rule.SupportedFunctions
                    If Not _functionMap.ContainsKey(f) Then
                        _functionMap(f) = New List(Of IBiochemicalRule)
                    End If
                    Call _functionMap(f).Add(rule)
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
                If rule.SupportedFunctions Is Nothing OrElse rule.SupportedFunctions.Length = 0 Then
                    rule.Execute(cell, env)
                End If
            Next
        End Sub

        ''' <summary>
        ''' [v2.0] 执行环境级别的规则（不针对特定细胞）
        ''' </summary>
        Public Sub ExecuteEnvironmentRules(env As NaturalEnvironment, iteration As Long)
            For Each rule As IBiochemicalRule In Rules
                If TypeOf rule Is NutrientReplenishmentRule Then
                    DirectCast(rule, NutrientReplenishmentRule).ExecuteEnvironment(env)
                ElseIf TypeOf rule Is DiffusionRule Then
                    DirectCast(rule, DiffusionRule).ExecuteEnvironment(env)
                ElseIf TypeOf rule Is TemperatureRule Then
                    DirectCast(rule, TemperatureRule).ExecuteEnvironment(env, iteration)
                End If
            Next
        End Sub
    End Class
End Namespace
