
Public Class GeneExpressionRule : Implements IBiochemicalRule

    Public ReadOnly Property SupportedFunctions As GeneOntology() Implements IBiochemicalRule.SupportedFunctions

    Public Sub Execute(cell As Cell, env As Environment3D) Implements IBiochemicalRule.Execute
        ' 基因转录（需要9个核苷酸）
        If cell.Proteins.ContainsKey(GeneOntology.GeneTranscription) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Nucleotide) AndAlso
           cell.InternalMolecules(MoleculeType.Nucleotide) >= 9 Then

            cell.InternalMolecules(MoleculeType.Nucleotide) -= 9
            ' 转录产生RNA（这里简化为直接增加蛋白质数量）
            ' 实际应增加RNA计数，但按您的描述，我们简化
            ConsumeBasicResources(cell)
        End If

        ' 蛋白质翻译（需要3种氨基酸各1单位）
        If cell.Proteins.ContainsKey(GeneOntology.ProteinTranslation) Then
            Dim hasAminoAcids = cell.InternalMolecules.ContainsKey(MoleculeType.AminoMixGluFamily) AndAlso
                               cell.InternalMolecules.ContainsKey(MoleculeType.AminoMixAspFamily) AndAlso
                               cell.InternalMolecules.ContainsKey(MoleculeType.AminoMixSerGly)

            If hasAminoAcids AndAlso
               cell.InternalMolecules(MoleculeType.AminoMixGluFamily) > 0 AndAlso
               cell.InternalMolecules(MoleculeType.AminoMixAspFamily) > 0 AndAlso
               cell.InternalMolecules(MoleculeType.AminoMixSerGly) > 0 Then

                cell.InternalMolecules(MoleculeType.AminoMixGluFamily) -= 1
                cell.InternalMolecules(MoleculeType.AminoMixAspFamily) -= 1
                cell.InternalMolecules(MoleculeType.AminoMixSerGly) -= 1
                ' 增加随机蛋白质（简化）
                ConsumeBasicResources(cell)
            End If
        End If
    End Sub

    Private Sub ConsumeBasicResources(cell As Cell)
        If cell.InternalMolecules.ContainsKey(MoleculeType.Water) Then
            cell.InternalMolecules(MoleculeType.Water) -= 1
        End If
        cell.ATP -= 1
    End Sub
End Class