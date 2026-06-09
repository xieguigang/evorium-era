Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    Public Class EnergyMetabolismRule : Implements IBiochemicalRule

        Public ReadOnly Property SupportedFunctions As GeneOntology() Implements IBiochemicalRule.SupportedFunctions

        Sub New()
            SupportedFunctions = {GeneOntology.AerobicEnergyMetabolismATP, GeneOntology.AnaerobicEnergyMetabolismATP}
        End Sub

        Public Sub Execute(cell As Cell, env As NaturalEnvironment) Implements IBiochemicalRule.Execute
            ' 需氧能量代谢
            If cell.HasFunction(GeneOntology.AerobicEnergyMetabolismATP) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Glucose) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Oxygen) Then

                If cell.InternalMolecules(MoleculeType.Glucose) > 0 AndAlso
               cell.InternalMolecules(MoleculeType.Oxygen) > 0 Then

                    cell.InternalMolecules(MoleculeType.Glucose) -= 1
                    cell.InternalMolecules(MoleculeType.Oxygen) -= 1
                    cell.ATP = Math.Min(cell.ATP + 12, 1000)
                    env.AddMolecule(cell, MoleculeType.CarbonDioxide, 6)
                    ConsumeBasicResources(cell)
                End If
            End If

            ' 厌氧能量代谢
            If cell.HasFunction(GeneOntology.AnaerobicEnergyMetabolismATP) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Acetate) Then

                If cell.InternalMolecules(MoleculeType.Acetate) > 0 AndAlso
               Not cell.InternalMolecules.ContainsKey(MoleculeType.Oxygen) Then

                    cell.InternalMolecules(MoleculeType.Acetate) -= 1
                    cell.ATP = Math.Min(cell.ATP + 5, 1000)
                    env.AddMolecule(cell, MoleculeType.CarbonDioxide, 2)
                    env.AddMolecule(cell, MoleculeType.HydrogenIon, 2)
                    ConsumeBasicResources(cell)
                End If
            End If
        End Sub

        Private Sub ConsumeBasicResources(cell As Cell)
            If cell.InternalMolecules.ContainsKey(MoleculeType.Water) Then
                cell.InternalMolecules(MoleculeType.Water) -= 1
            End If
        End Sub
    End Class
End Namespace