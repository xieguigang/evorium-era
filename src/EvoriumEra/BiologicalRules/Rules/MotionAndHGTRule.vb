



Public Class MotionAndHGTRule : Implements IBiochemicalRule
    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        ' 细胞鞭毛运动
        If cell.Proteins.ContainsKey(GeneOntology.FlagellarMovement) Then
            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
            Dim neighbors = env.GetNeighbors(voxel).Where(Function(v) v.Occupant Is Nothing).ToList()

            If neighbors.Any() Then
                Dim target = neighbors(rng.Next(neighbors.Count))
                env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z).Occupant = Nothing
                cell.Position = (target.X, target.Y, target.Z)
                target.Occupant = cell
                ConsumeBasicResources(cell)
            End If
        End If

        ' 质粒交换（相邻细胞）
        Dim currentVoxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
        For Each neighbor In env.GetNeighbors(currentVoxel)
            If neighbor.Occupant IsNot Nothing Then
                If rng.NextDouble() < 0.01 Then ' 1%概率
                    ExchangePlasmids(cell, neighbor.Occupant, rng)
                End If
            End If
        Next
    End Sub

    Private Sub ExchangePlasmids(cell1 As Cell, cell2 As Cell, rng As Random)
        If cell1.Plasmids.Any() AndAlso cell2.Plasmids.Any() Then
            Dim plasmid1 = cell1.Plasmids(rng.Next(cell1.Plasmids.Count))
            Dim plasmid2 = cell2.Plasmids(rng.Next(cell2.Plasmids.Count))

            cell1.Plasmids.Remove(plasmid1)
            cell2.Plasmids.Remove(plasmid2)
            cell1.Plasmids.Add(plasmid2)
            cell2.Plasmids.Add(plasmid1)
        End If
    End Sub

    Private Sub ConsumeBasicResources(cell As Cell)
        If cell.InternalMolecules.ContainsKey(MoleculeType.Water) Then
            cell.InternalMolecules(MoleculeType.Water) -= 1
        End If
        cell.ATP -= 1
    End Sub
End Class