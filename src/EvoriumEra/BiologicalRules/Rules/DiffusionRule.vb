Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports rng = Microsoft.VisualBasic.Math.RandomExtensions

Namespace BiologicalRules.Rules

    Public Class DiffusionRule : Implements IBiochemicalRule

        Public ReadOnly Property SupportedFunctions As GeneOntology() Implements IBiochemicalRule.SupportedFunctions

        Sub New()
        End Sub

        Public Sub Execute(cell As Cell, env As NaturalEnvironment) Implements IBiochemicalRule.Execute
            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
            Dim neighbors = env.GetNeighbors(voxel)

            For Each neighbor In neighbors
                ' 被动扩散分子
                Dim diffusable = {MoleculeType.Oxygen, MoleculeType.Water, MoleculeType.HydrogenIon,
                             MoleculeType.HydroxideIon, MoleculeType.CarbonDioxide,
                             MoleculeType.CarbonSource, MoleculeType.NitrogenSource}

                For Each mol In diffusable
                    If voxel.ExternalMolecules.ContainsKey(mol) AndAlso
                   neighbor.ExternalMolecules.ContainsKey(mol) Then

                        Dim diff = voxel.ExternalMolecules(mol) - neighbor.ExternalMolecules(mol)
                        If Math.Abs(diff) > 0 Then
                            Dim transfer = CInt(Math.Sign(diff) * Math.Min(Math.Abs(diff) * 0.1, rng.NextInteger(1, 6)))
                            voxel.ExternalMolecules(mol) -= transfer
                            neighbor.ExternalMolecules(mol) += transfer
                        End If
                    End If
                Next
            Next
        End Sub
    End Class

End Namespace