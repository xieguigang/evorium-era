

Public Class MutationRule : Implements IBiochemicalRule
    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        ' DNA复制时突变（简化：每次复制有1%突变率）
        If cell.Proteins.ContainsKey(GeneOntology.ReplicateDNA) AndAlso rng.NextDouble() < 0.01 Then
            Dim replicons = New List(Of Replicon) From {cell.Genome}
            replicons.AddRange(cell.Plasmids)

            For Each replicon In replicons
                If replicon.Genes.Any() Then
                    Select Case rng.Next(3)
                        Case 0 ' 缺失突变
                            Dim index = rng.Next(replicon.Genes.Count)
                            replicon.Genes.RemoveAt(index)
                        Case 1 ' 插入突变
                            Dim gene = replicon.Genes(rng.Next(replicon.Genes.Count))
                            replicon.Genes.Add(New Gene With {.FunctionOntology = gene.FunctionOntology})
                        Case 2 ' 功能突变
                            Dim index = rng.Next(replicon.Genes.Count)
                            Dim allFunctions = [Enum].GetValues(GetType(GeneOntology)).Cast(Of GeneOntology)().ToList()
                            replicon.Genes(index).FunctionOntology = allFunctions(rng.Next(allFunctions.Count))
                    End Select
                End If
            Next
        End If
    End Sub
End Class