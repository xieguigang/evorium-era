Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    Public Class QuorumSensingAndBiofilmRule : Implements IBiochemicalRule


        Public ReadOnly Property SupportedFunctions As GeneOntology() Implements IBiochemicalRule.SupportedFunctions

        Sub New()
            SupportedFunctions = {
            GeneOntology.QuorumSensing,
            GeneOntology.SignalMoleculeSynthesis,
            GeneOntology.SecondaryMetaboliteSynthesis,
            GeneOntology.BiofilmSynthesis
        }
        End Sub

        Public Sub Execute(cell As Cell, env As Environment3D) Implements IBiochemicalRule.Execute
            ' 信号分子合成
            If cell.Proteins.ContainsKey(GeneOntology.SignalMoleculeSynthesis) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.CarbonSource) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.AminoMixSerGly) Then

                cell.InternalMolecules(MoleculeType.CarbonSource) -= 1
                cell.InternalMolecules(MoleculeType.AminoMixSerGly) -= 1
                AddMolecule(cell, MoleculeType.SignalMolecule, 1)
                ConsumeBasicResources(cell)
            End If

            ' 群体感应
            If cell.Proteins.ContainsKey(GeneOntology.QuorumSensing) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.SignalMolecule) AndAlso
           cell.InternalMolecules(MoleculeType.SignalMolecule) >= 100 Then

                ' 强制执行次级代谢产物合成或生物膜合成
                If cell.Proteins.ContainsKey(GeneOntology.SecondaryMetaboliteSynthesis) Then
                    ExecuteSecondaryMetaboliteSynthesis(cell)
                ElseIf cell.Proteins.ContainsKey(GeneOntology.BiofilmSynthesis) Then
                    ExecuteBiofilmSynthesis(cell, env)
                End If
            End If
        End Sub

        Private Sub ExecuteSecondaryMetaboliteSynthesis(cell As Cell)
            If cell.InternalMolecules.ContainsKey(MoleculeType.Acetate) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.AminoMixGluFamily) AndAlso
           cell.InternalMolecules(MoleculeType.Acetate) >= 2 AndAlso
           cell.InternalMolecules(MoleculeType.AminoMixGluFamily) >= 1 Then

                cell.InternalMolecules(MoleculeType.Acetate) -= 2
                cell.InternalMolecules(MoleculeType.AminoMixGluFamily) -= 1
                AddMolecule(cell, MoleculeType.SecondaryMetabolite, 1)
            End If
        End Sub

        Private Sub ExecuteBiofilmSynthesis(cell As Cell, env As Environment3D)
            If cell.InternalMolecules.ContainsKey(MoleculeType.NitrogenSource) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.AminoMixAspFamily) AndAlso
           cell.InternalMolecules(MoleculeType.NitrogenSource) >= 10 AndAlso
           cell.InternalMolecules(MoleculeType.AminoMixAspFamily) >= 5 Then

                cell.InternalMolecules(MoleculeType.NitrogenSource) -= 10
                cell.InternalMolecules(MoleculeType.AminoMixAspFamily) -= 5
                Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
                voxel.HasBiofilm = True
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

        Private Function QuorumProbability(cell As Cell) As Double
            Const n As Double = 2.0   ' Hill系数
            Const K As Double = 100.0 ' 半饱和常数

            Dim S = cell.InternalMolecules.GetValueOrDefault(MoleculeType.SignalMolecule, 0)
            Return Math.Pow(S, n) / (Math.Pow(K, n) + Math.Pow(S, n))
        End Function


    End Class
End Namespace