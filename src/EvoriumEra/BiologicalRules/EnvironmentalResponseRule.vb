
Public Class EnvironmentalResponseRule : Implements IBiochemicalRule
    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        ' 酸代谢
        If cell.Proteins.ContainsKey(GeneFunction.AcidMetabolism) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.HydrogenIon) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.CarbonSource) Then

            If cell.InternalMolecules(MoleculeType.HydrogenIon) >= 20 Then
                cell.InternalMolecules(MoleculeType.HydrogenIon) -= 1
                cell.InternalMolecules(MoleculeType.CarbonSource) -= 1
                AddMolecule(cell, MoleculeType.Water, 1)
                cell.ATP += 1
            End If
        End If

        ' 碱代谢
        If cell.Proteins.ContainsKey(GeneFunction.BaseMetabolism) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.HydroxideIon) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.NitrogenSource) Then

            If cell.InternalMolecules(MoleculeType.HydroxideIon) >= 20 Then
                cell.InternalMolecules(MoleculeType.HydroxideIon) -= 1
                cell.InternalMolecules(MoleculeType.NitrogenSource) -= 1
                AddMolecule(cell, MoleculeType.Water, 1)
                cell.ATP += 1
            End If
        End If

        ' 抗生素抑制
        If cell.InternalMolecules.ContainsKey(MoleculeType.Antibiotic) AndAlso
           cell.InternalMolecules(MoleculeType.Antibiotic) > 0 Then
            ' 除降解抗生素外，所有蛋白质失活
            ' 这里简化处理：减少ATP消耗但不执行其他功能
            cell.ATP = Math.Max(0, cell.ATP - 1)
        End If
    End Sub

    Private Sub AddMolecule(cell As Cell, type As MoleculeType, amount As Integer)
        If Not cell.InternalMolecules.ContainsKey(type) Then
            cell.InternalMolecules(type) = 0
        End If
        cell.InternalMolecules(type) += amount
    End Sub
End Class