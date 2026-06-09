Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports rng = Microsoft.VisualBasic.Math.RandomExtensions

Namespace BiologicalRules

    ''' <summary>
    ''' 模拟计算的生命活动规则 v2.0
    ''' 
    ''' 核心改进：
    ''' 1. 完整代谢链：glucose → pyruvate → acetate → ATP
    ''' 2. 基因必须转录为蛋白质才能行使功能
    ''' 3. 基因组维护成本：基因越多，每迭代消耗ATP越多
    ''' 4. 代谢溢流：中间产物超出阈值自动分泌，驱动交叉喂养
    ''' 5. 环境梯度：氧气随深度递减，营养热点分布
    ''' 6. 营养补充：每迭代向环境补充营养
    ''' 7. 细胞裂解：死亡细胞释放内容物
    ''' 
    ''' 模拟计算的生命活动规则 v3.0
    ''' 
    ''' v3.0新增：
    ''' 1. 温度系统：昼夜循环、深度梯度、代谢产热、蛋白热失活/冷休克
    ''' 2. 渗透压系统：离子强度、渗透压调节、相容溶质
    ''' 3. 扩展代谢网络：更多碳代谢路径、硫/铁/磷循环、扩展氨基酸和次级代谢
    ''' </summary>
    Public MustInherit Class IBiochemicalRule

        Public ReadOnly Property SupportedFunctions As GeneOntology()

        Sub New(ParamArray terms As GeneOntology())
            SupportedFunctions = terms
        End Sub

        MustOverride Sub Execute(cell As Cell, env As NaturalEnvironment)

        Public Overrides Function ToString() As String
            Return SupportedFunctions.Select(Function(go) go.Description).ToArray().JoinBy(", ")
        End Function

        ''' <summary>
        ''' 检查并消耗基础资源（1 ATP + 1 水），能量代谢功能豁免ATP消耗
        ''' 
        ''' [v3.0] 增加温度活性修正：蛋白活性低时功能执行概率降低
        ''' </summary>
        Protected Shared Function ConsumeBasicResources(cell As Cell, Optional exemptATP As Boolean = False) As Boolean
            ' 检查水
            If cell.GetMoleculeAmount(MoleculeType.Water) <= 0 Then Return False
            ' 检查ATP（能量代谢功能豁免）
            If Not exemptATP AndAlso cell.ATP <= 0 Then Return False

            ' [v3.0] 温度活性修正：蛋白活性低时，功能执行有概率失败
            If cell.ProteinActivityFactor < 1.0 Then
                Dim r = rng.NextDouble()
                If r > cell.ProteinActivityFactor Then
                    ' 蛋白活性不足，功能执行失败，但仍消耗ATP
                    If Not exemptATP Then cell.ATP -= 1
                    cell.AddMoleculeInternal(MoleculeType.Water, -1)
                    Return False
                End If
            End If

            ' 消耗水
            cell.AddMoleculeInternal(MoleculeType.Water, -1)
            ' 消耗ATP（能量代谢功能豁免）
            If Not exemptATP Then cell.ATP -= 1
            ' 产生CO2
            cell.AddMoleculeInternal(MoleculeType.CarbonDioxide, 1)

            ' [v3.0] 代谢产热：每次ATP消耗产生微量热量
            cell.InternalTemperature += 0.02

            Return True
        End Function

    End Class
End Namespace
