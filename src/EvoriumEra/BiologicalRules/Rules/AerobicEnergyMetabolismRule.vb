Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' 有氧能量代谢规则
    ''' </summary>
    Public Class AerobicEnergyMetabolismRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(GeneOntology.AerobicEnergyMetabolismATP)
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            If cell.InternalMolecules.ContainsKey(MoleculeType.Glucose) AndAlso
                cell.InternalMolecules.ContainsKey(MoleculeType.Oxygen) AndAlso
                cell.HasFunction(GeneOntology.AerobicEnergyMetabolismATP) Then

                If cell.InternalMolecules(MoleculeType.Glucose) > 0 AndAlso cell.InternalMolecules(MoleculeType.Oxygen) > 0 Then
                    cell.InternalMolecules(MoleculeType.Glucose) -= 1
                    cell.InternalMolecules(MoleculeType.Oxygen) -= 1
                    cell.ATP = Math.Min(cell.ATP + 12, 1000)
                    env.AddMolecule(cell, MoleculeType.CarbonDioxide, 6)
                End If
            End If
        End Sub
    End Class
End Namespace