Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    Public Class TransportRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(GeneOntology.Endocytosis, GeneOntology.Exocytosis)
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)

            ' 物质内吞
            If cell.HasFunction(GeneOntology.Endocytosis) Then
                For Each moleculeType In voxel.ExternalMolecules.Keys.ToList()
                    If Not IsPassiveDiffusion(moleculeType) Then
                        Dim amount = Math.Min(voxel.ExternalMolecules(moleculeType), 5)
                        If amount > 0 Then
                            voxel.ExternalMolecules(moleculeType) -= amount
                            AddMolecule(cell, moleculeType, amount)
                            ConsumeBasicResources(cell)
                        End If
                    End If
                Next
            End If

            ' 物质分泌
            If cell.HasFunction(GeneOntology.Exocytosis) Then
                For Each moleculeType In cell.InternalMolecules.Keys.ToList()
                    If Not IsPassiveDiffusion(moleculeType) Then
                        Dim amount = Math.Min(cell.InternalMolecules(moleculeType), 5)
                        If amount > 0 Then
                            cell.InternalMolecules(moleculeType) -= amount
                            AddMolecule(voxel, moleculeType, amount)
                            ConsumeBasicResources(cell)
                        End If
                    End If
                Next
            End If
        End Sub

        Private Function IsPassiveDiffusion(type As MoleculeType) As Boolean
            Return type = MoleculeType.Oxygen OrElse
               type = MoleculeType.Water OrElse
               type = MoleculeType.HydrogenIon OrElse
               type = MoleculeType.HydroxideIon OrElse
               type = MoleculeType.CarbonDioxide OrElse
               type = MoleculeType.CarbonSource OrElse
               type = MoleculeType.NitrogenSource
        End Function

        Private Sub AddMolecule(container As Object, type As MoleculeType, amount As Integer)
            If container.GetType() = GetType(Cell) Then
                Dim cell = CType(container, Cell)
                If Not cell.InternalMolecules.ContainsKey(type) Then
                    cell.InternalMolecules(type) = 0
                End If
                cell.InternalMolecules(type) += amount
            ElseIf container.GetType() = GetType(Voxel) Then
                Dim voxel = CType(container, Voxel)
                If Not voxel.ExternalMolecules.ContainsKey(type) Then
                    voxel.ExternalMolecules(type) = 0
                End If
                voxel.ExternalMolecules(type) += amount
            End If
        End Sub

        Private Sub ConsumeBasicResources(cell As Cell)
            If cell.InternalMolecules.ContainsKey(MoleculeType.Water) Then
                cell.InternalMolecules(MoleculeType.Water) -= 1
            End If
            cell.ATP -= 1
        End Sub
    End Class
End Namespace