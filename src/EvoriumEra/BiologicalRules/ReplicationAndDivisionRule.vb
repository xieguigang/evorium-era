Public Class ReplicationAndDivisionRule : Implements IBiochemicalRule
    Public ReadOnly Property SupportedFunctions As List(Of GeneFunction) _
        Implements IBiochemicalRule.SupportedFunctions
        Get
            Return New List(Of GeneFunction) From {
                GeneFunction.CellDivision
            }
        End Get
    End Property

    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        ' DNA复制（需要n*9 * 2个核苷酸）
        Dim totalGenes = cell.Genome.Genes.Count + cell.Plasmids.Sum(Function(p) p.Genes.Count)
        Dim requiredNucleotides = totalGenes * 9 * 2

        If cell.Proteins.ContainsKey(GeneFunction.ReplicateDNA) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Nucleotide) AndAlso
           cell.InternalMolecules(MoleculeType.Nucleotide) >= requiredNucleotides Then

            cell.InternalMolecules(MoleculeType.Nucleotide) -= requiredNucleotides
            ' 复制DNA（简化：标记已复制）
            ConsumeBasicResources(cell)
        End If

        ' 细胞分裂
        If cell.Proteins.ContainsKey(GeneFunction.CellDivision) Then
            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
            Dim neighbors = env.GetNeighbors(voxel).Where(Function(v) v.Occupant Is Nothing).ToList()

            If neighbors.Any() AndAlso cell.InternalMolecules.ContainsKey(MoleculeType.Nucleotide) AndAlso
               cell.InternalMolecules(MoleculeType.Nucleotide) >= requiredNucleotides Then

                ' 6:4分配
                Dim newCell As New Cell With {
                    .Position = (neighbors(rng.Next(neighbors.Count)).X,
                                neighbors(rng.Next(neighbors.Count)).Y,
                                neighbors(rng.Next(neighbors.Count)).Z),
                    .Genome = CloneReplicon(cell.Genome),
                    .Plasmids = cell.Plasmids.Select(Function(p) CloneReplicon(p)).ToList(),
                    .ParentID = cell.ID,
                    .Generation = cell.Generation + 1
                }

                ' 分配分子（简化）
                DistributeMolecules(cell, newCell)
                env.Grid(newCell.Position.X, newCell.Position.Y, newCell.Position.Z).Occupant = newCell
                ConsumeBasicResources(cell)
            End If
        End If
    End Sub

    Private Function CloneReplicon(r As Replicon) As Replicon
        Return New Replicon With {.Genes = r.Genes.Select(Function(g) New Gene With {.FunctionTag = g.FunctionTag}).ToList()}
    End Function

    Private Sub DistributeMolecules(parent As Cell, child As Cell)
        ' 简化：按6:4分配所有分子
        For Each kvp In parent.InternalMolecules.ToList()
            Dim parentAmount = CInt(kvp.Value * 0.6)
            Dim childAmount = kvp.Value - parentAmount
            parent.InternalMolecules(kvp.Key) = parentAmount
            child.InternalMolecules(kvp.Key) = childAmount
        Next
    End Sub

    Private Sub ConsumeBasicResources(cell As Cell)
        If cell.InternalMolecules.ContainsKey(MoleculeType.Water) Then
            cell.InternalMolecules(MoleculeType.Water) -= 1
        End If
        cell.ATP -= 1
    End Sub
End Class