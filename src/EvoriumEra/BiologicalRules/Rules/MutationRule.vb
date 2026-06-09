Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports rng = Microsoft.VisualBasic.Math.RandomExtensions

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v2.0 改进] 突变规则
    ''' 
    ''' 改进：
    ''' 1. 提高突变率到3%（原1%太低，难以产生多样性）
    ''' 2. 增加基因重复突变（复制已有基因）
    ''' 3. 增加基因缺失突变（丢失基因，驱动基因组精简）
    ''' 4. 功能突变：随机改变基因功能
    ''' 
    ''' 基因重复是演化创新的关键来源；
    ''' 基因缺失在资源受限时驱动基因组精简，促进代谢专化。
    ''' </summary>
    Public Class MutationRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(GeneOntology.ReplicateDNA)
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            If Not cell.HasFunction(GeneOntology.ReplicateDNA) Then Return

            Dim replicons = New List(Of Replicon) From {cell.Genome}
            replicons.AddRange(cell.Plasmids)

            For Each replicon In replicons
                If Not replicon.Genes.Any() Then Continue For

                ' 基础突变率 3%
                If rng.NextDouble() < 0.03 Then
                    Dim mutationType = rng.NextDouble()

                    If mutationType < 0.35 Then
                        ' 基因重复（35%概率）—— 演化创新的关键
                        Dim sourceGene = replicon.Genes(rng.Next(replicon.Genes.Count))
                        replicon.Genes.Add(New Gene With {.FunctionOntology = sourceGene.FunctionOntology})

                    ElseIf mutationType < 0.55 Then
                        ' 基因缺失（20%概率）—— 驱动基因组精简
                        If replicon.Genes.Count > 3 Then ' 保留最少3个基因
                            Dim index = rng.Next(replicon.Genes.Count)
                            replicon.Genes.RemoveAt(index)
                        End If

                    ElseIf mutationType < 0.85 Then
                        ' 功能突变（30%概率）—— 随机改变基因功能
                        Dim index = rng.Next(replicon.Genes.Count)
                        Dim allFunctions = [Enum].GetValues(GetType(GeneOntology)).Cast(Of GeneOntology)().ToList()
                        replicon.Genes(index).FunctionOntology = allFunctions(rng.Next(allFunctions.Count))

                    Else
                        ' 插入突变（15%概率）—— 插入全新随机基因
                        Dim allFunctions = [Enum].GetValues(GetType(GeneOntology)).Cast(Of GeneOntology)().ToList()
                        replicon.Genes.Add(New Gene With {.FunctionOntology = allFunctions(rng.Next(allFunctions.Count))})
                    End If
                End If
            Next
        End Sub
    End Class
End Namespace
