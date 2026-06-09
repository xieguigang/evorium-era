Namespace BiologicalRules.Rules

    Public Class SynthesisAndDegradationRule : Implements IBiochemicalRule

        Public ReadOnly Property SupportedFunctions As GeneOntology() Implements IBiochemicalRule.SupportedFunctions

        Public Sub Execute(cell As Cell, env As Environment3D) Implements IBiochemicalRule.Execute
            ' 降解大分子
            If cell.Proteins.ContainsKey(GeneOntology.DegradeMacromolecule) Then
                ' 降解细胞内大分子
                Dim macromolecules = {MoleculeType.SecondaryMetabolite, MoleculeType.Nucleotide,
                                 MoleculeType.DNA, MoleculeType.AminoMixGluFamily,
                                 MoleculeType.Biofilm}

                For Each mol In macromolecules
                    If cell.InternalMolecules.ContainsKey(mol) AndAlso cell.InternalMolecules(mol) > 0 Then
                        cell.InternalMolecules(mol) -= 1
                        AddMolecule(cell, MoleculeType.CarbonSource, 1)
                        AddMolecule(cell, MoleculeType.NitrogenSource, 1)
                        Exit For
                    End If
                Next
                ConsumeBasicResources(cell)
            End If

            ' 合成抗生素
            If cell.Proteins.ContainsKey(GeneOntology.SynthesizeAntibiotic) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Acetate) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.NitrogenSource) Then

                cell.InternalMolecules(MoleculeType.Acetate) -= 1
                cell.InternalMolecules(MoleculeType.NitrogenSource) -= 1
                AddMolecule(cell, MoleculeType.Antibiotic, 1)
                ConsumeBasicResources(cell)
            End If

            ' 降解抗生素
            If cell.Proteins.ContainsKey(GeneOntology.DegradeAntibiotic) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Antibiotic) Then

                cell.InternalMolecules(MoleculeType.Antibiotic) -= 1
                AddMolecule(cell, MoleculeType.Acetate, 1)
                AddMolecule(cell, MoleculeType.NitrogenSource, 1)
                AddMolecule(cell, MoleculeType.HydroxideIon, 2)
                ConsumeBasicResources(cell)
            End If
        End Sub

        Private Sub AddMolecule(cell As Cell, type As MoleculeType, amount As Integer)
            If Not cell.InternalMolecules.ContainsKey(type) Then
                cell.InternalMolecules(type) = 0
            End If
            cell.InternalMolecules(type) += amount
        End Sub

        Private Sub ConsumeBasicResources(cell As Cell)
            If cell.InternalMolecules.ContainsKey(MoleculeType.Water) Then
                cell.InternalMolecules(MoleculeType.Water) -= 1
            End If
            cell.ATP -= 1
        End Sub

        Private Sub SecondaryMetaboliteKinetics(cell As Cell, env As Environment3D)
            Const Vmax As Integer = 3
            Const Km As Integer = 10

            Dim acetate = cell.InternalMolecules.GetValueOrDefault(MoleculeType.Acetate, 0)
            Dim glu = cell.InternalMolecules.GetValueOrDefault(MoleculeType.AminoMixGluFamily, 0)

            If acetate < 2 OrElse glu < 1 Then Return

            Dim rate = CInt(Vmax * acetate / (Km + acetate))
            rate = Math.Min(rate, Math.Min(acetate \ 2, glu))

            env.AddMolecule(cell, MoleculeType.Acetate, -2 * rate)
            env.AddMolecule(cell, MoleculeType.AminoMixGluFamily, -rate)
            env.AddMolecule(cell, MoleculeType.SecondaryMetabolite, rate)
        End Sub

        Private Sub BiofilmKinetics(cell As Cell, env As Environment3D)
            Const rho As Double = 1.0
            Const KN As Double = 50.0
            Const KA As Double = 10.0

            Dim N = cell.InternalMolecules.GetValueOrDefault(MoleculeType.NitrogenSource, 0)
            Dim A = cell.InternalMolecules.GetValueOrDefault(MoleculeType.AminoMixAspFamily, 0)

            Dim prob = rho * (N / (KN + N)) * (A / (KA + A))

            If cell.ATP < 1 OrElse prob < 0.5 Then Return

            env.AddMolecule(cell, MoleculeType.NitrogenSource, -10)
            env.AddMolecule(cell, MoleculeType.AminoMixAspFamily, -5)
            cell.ATP -= 1

            Dim v = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
            v.HasBiofilm = True
        End Sub
    End Class
End Namespace