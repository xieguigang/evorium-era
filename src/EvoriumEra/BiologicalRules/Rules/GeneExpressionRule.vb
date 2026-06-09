Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports RNG = Microsoft.VisualBasic.Math.RandomExtensions

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v2.0 改进] 基因表达规则
    ''' 
    ''' 核心改进：基因必须转录为对应的蛋白质才能行使功能。
    ''' 蛋白质是功能的真正执行者，而非基因本身。
    ''' 
    ''' 流程：
    ''' 1. 基因转录：消耗9个核苷酸 → 产生1个对应蛋白质
    ''' 2. 蛋白质翻译：消耗3种氨基酸各1个 → 产生1个对应蛋白质
    ''' 
    ''' 每次执行只转录+翻译一个基因，按优先级选择。
    ''' </summary>
    Public Class GeneExpressionRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(GeneOntology.GeneTranscription, GeneOntology.ProteinTranslation)
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            ' 选择一个需要表达的基因（基于当前需求优先级）
            Dim geneToExpress = SelectGeneForExpression(cell)

            If geneToExpress Is Nothing Then Return

            ' ===== 基因转录 =====
            If cell.HasFunction(GeneOntology.GeneTranscription) Then
                Dim nucleotides = cell.GetMoleculeAmount(MoleculeType.Nucleotide)

                If nucleotides >= 9 Then
                    cell.AddMoleculeInternal(MoleculeType.Nucleotide, -9)

                    ' 转录产生对应蛋白质
                    If Not cell.Proteins.ContainsKey(geneToExpress.FunctionOntology) Then
                        cell.Proteins(geneToExpress.FunctionOntology) = 0
                    End If
                    cell.Proteins(geneToExpress.FunctionOntology) += 1

                    ConsumeBasicResources(cell)
                End If
            End If

            ' ===== 蛋白质翻译 =====
            If cell.HasFunction(GeneOntology.ProteinTranslation) Then
                Dim gluAA = cell.GetMoleculeAmount(MoleculeType.AminoMixGluFamily)
                Dim aspAA = cell.GetMoleculeAmount(MoleculeType.AminoMixAspFamily)
                Dim serAA = cell.GetMoleculeAmount(MoleculeType.AminoMixSerGly)

                If gluAA > 0 AndAlso aspAA > 0 AndAlso serAA > 0 Then
                    cell.AddMoleculeInternal(MoleculeType.AminoMixGluFamily, -1)
                    cell.AddMoleculeInternal(MoleculeType.AminoMixAspFamily, -1)
                    cell.AddMoleculeInternal(MoleculeType.AminoMixSerGly, -1)

                    ' 翻译产生对应蛋白质
                    If Not cell.Proteins.ContainsKey(geneToExpress.FunctionOntology) Then
                        cell.Proteins(geneToExpress.FunctionOntology) = 0
                    End If
                    cell.Proteins(geneToExpress.FunctionOntology) += 1

                    ConsumeBasicResources(cell)
                End If
            End If
        End Sub

        ''' <summary>
        ''' 基于当前细胞状态选择最需要表达的基因
        ''' 优先级：能量代谢 > 物质运输 > 代谢通路 > 其他
        ''' </summary>
        Private Function SelectGeneForExpression(cell As Cell) As Gene
            Dim allGenes = cell.Genome.Genes.ToList()
            For Each plasmid In cell.Plasmids
                allGenes.AddRange(plasmid.Genes)
            Next

            If allGenes.Count = 0 Then Return Nothing

            ' 构建优先级权重
            Dim weighted = New List(Of (gene As Gene, weight As Double))

            For Each g In allGenes
                Dim w As Double = 1.0

                ' ATP低时，能量代谢基因优先
                If cell.ATP < 200 Then
                    If g.FunctionOntology = GeneOntology.AerobicEnergyMetabolismATP OrElse
                       g.FunctionOntology = GeneOntology.AnaerobicEnergyMetabolismATP OrElse
                       g.FunctionOntology = GeneOntology.GlucoseConversionEnzyme OrElse
                       g.FunctionOntology = GeneOntology.AcetateEnzyme OrElse
                       g.FunctionOntology = GeneOntology.LactateDehydrogenase Then
                        w *= 5.0
                    End If
                End If

                ' 缺少营养时，运输基因优先
                If cell.GetMoleculeAmount(MoleculeType.Glucose) < 5 OrElse
                   cell.GetMoleculeAmount(MoleculeType.CarbonSource) < 5 Then
                    If g.FunctionOntology = GeneOntology.Endocytosis OrElse
                       g.FunctionOntology = GeneOntology.FlagellarMovement Then
                        w *= 3.0
                    End If
                End If

                ' 有抗生素时，降解抗生素优先
                If cell.GetMoleculeAmount(MoleculeType.Antibiotic) > 0 Then
                    If g.FunctionOntology = GeneOntology.DegradeAntibiotic Then
                        w *= 10.0
                    End If
                End If

                ' 已有大量该蛋白质时降低优先级
                If cell.Proteins.ContainsKey(g.FunctionOntology) AndAlso
                   cell.Proteins(g.FunctionOntology) > 5 Then
                    w *= 0.3
                End If

                weighted.Add((g, w))
            Next

            ' 加权随机选择
            Dim totalWeight = weighted.Sum(Function(x) x.weight)
            Dim r = RNG.NextDouble() * totalWeight
            Dim cumulative = 0.0

            For Each item In weighted
                cumulative += item.weight
                If r <= cumulative Then
                    Return item.gene
                End If
            Next

            Return weighted.Last.gene
        End Function

    End Class
End Namespace
