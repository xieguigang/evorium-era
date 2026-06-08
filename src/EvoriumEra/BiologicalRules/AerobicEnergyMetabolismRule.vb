Public Class AerobicEnergyMetabolismRule : Implements IBiochemicalRule
    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        If cell.InternalMolecules.ContainsKey(MoleculeType.Glucose) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Oxygen) Then

            If cell.InternalMolecules(MoleculeType.Glucose) > 0 AndAlso
               cell.InternalMolecules(MoleculeType.Oxygen) > 0 Then

                cell.InternalMolecules(MoleculeType.Glucose) -= 1
                cell.InternalMolecules(MoleculeType.Oxygen) -= 1
                cell.ATP = Math.Min(cell.ATP + 12, 1000)
                AddMolecule(cell, MoleculeType.CarbonDioxide, 6)
            End If
        End If
    End Sub
End Class