Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v2.0 改进] 合成与降解规则
    ''' 
    ''' 包含：降解大分子、合成抗生素、降解抗生素、
    ''' 氨基酸合成、核苷酸合成、固碳、细胞壁合成、生物膜合成、
    ''' 铁载体合成、次级代谢产物合成
    ''' </summary>
    Public Class SynthesisAndDegradationRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(
                GeneOntology.DegradeMacromolecule,
                GeneOntology.SynthesizeAntibiotic,
                GeneOntology.DegradeAntibiotic,
                GeneOntology.AminoMixGluFamilyEnzyme,
                GeneOntology.AminoMixAspFamilyEnzyme,
                GeneOntology.AminoMixSerGlyEnzyme,
                GeneOntology.NucleicAcidSynthesis,
                GeneOntology.CarbonFixation,
                GeneOntology.CellWallSynthesis,
                GeneOntology.SiderophoreSynthesis
            )
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            ' ===== 降解大分子 =====
            If cell.HasFunction(GeneOntology.DegradeMacromolecule) Then
                Dim macromolecules = {
                    MoleculeType.SecondaryMetabolite, MoleculeType.Nucleotide,
                    MoleculeType.DNA, MoleculeType.AminoMixGluFamily,
                    MoleculeType.Biofilm, MoleculeType.CellWall
                }

                For Each mol In macromolecules
                    Dim amount = cell.GetMoleculeAmount(mol)
                    If amount > 0 Then
                        If ConsumeBasicResources(cell) Then
                            cell.AddMoleculeInternal(mol, -1)
                            ' 降解产生碳源和氮源
                            cell.AddMoleculeInternal(MoleculeType.CarbonSource, 2)
                            cell.AddMoleculeInternal(MoleculeType.NitrogenSource, 1)
                        End If
                        Exit For ' 每次只降解一种
                    End If
                Next

                ' 降解环境中的生物膜
                Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
                If voxel.HasBiofilm Then
                    If ConsumeBasicResources(cell) Then
                        voxel.BiofilmStrength -= 20
                        If voxel.BiofilmStrength <= 0 Then
                            voxel.BiofilmStrength = 0
                            voxel.HasBiofilm = False
                        End If
                    End If
                End If
            End If

            ' ===== 氨基酸合成 =====
            ' Glu族：1 pyruvate + 1 氮源 → 2 Glu族氨基酸
            If cell.HasFunction(GeneOntology.AminoMixGluFamilyEnzyme) Then
                Dim pyruvate = cell.GetMoleculeAmount(MoleculeType.Pyruvate)
                Dim nitrogen = cell.GetMoleculeAmount(MoleculeType.NitrogenSource)
                If pyruvate > 0 AndAlso nitrogen > 0 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.Pyruvate, -1)
                        cell.AddMoleculeInternal(MoleculeType.NitrogenSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.AminoMixGluFamily, 2)
                    End If
                End If
            End If

            ' Asp族：1 acetate + 1 氮源 → 2 Asp族氨基酸
            If cell.HasFunction(GeneOntology.AminoMixAspFamilyEnzyme) Then
                Dim acetate = cell.GetMoleculeAmount(MoleculeType.Acetate)
                Dim nitrogen = cell.GetMoleculeAmount(MoleculeType.NitrogenSource)
                If acetate > 0 AndAlso nitrogen > 0 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.Acetate, -1)
                        cell.AddMoleculeInternal(MoleculeType.NitrogenSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.AminoMixAspFamily, 2)
                    End If
                End If
            End If

            ' Ser/Gly族：1 碳源 + 1 氮源 → 2 Ser/Gly族氨基酸
            If cell.HasFunction(GeneOntology.AminoMixSerGlyEnzyme) Then
                Dim carbon = cell.GetMoleculeAmount(MoleculeType.CarbonSource)
                Dim nitrogen = cell.GetMoleculeAmount(MoleculeType.NitrogenSource)
                If carbon > 0 AndAlso nitrogen > 0 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.NitrogenSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.AminoMixSerGly, 2)
                    End If
                End If
            End If

            ' ===== 核苷酸合成 =====
            If cell.HasFunction(GeneOntology.NucleicAcidSynthesis) Then
                Dim carbon = cell.GetMoleculeAmount(MoleculeType.CarbonSource)
                Dim nitrogen = cell.GetMoleculeAmount(MoleculeType.NitrogenSource)
                If carbon >= 2 AndAlso nitrogen >= 1 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, -2)
                        cell.AddMoleculeInternal(MoleculeType.NitrogenSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.Nucleotide, 3)
                    End If
                End If
            End If

            ' ===== 固碳 =====
            If cell.HasFunction(GeneOntology.CarbonFixation) Then
                Dim co2 = cell.GetMoleculeAmount(MoleculeType.CarbonDioxide)
                If co2 >= 3 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.CarbonDioxide, -3)
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, 1)
                        cell.ATP = Math.Min(cell.ATP + 1, 1000)
                    End If
                End If
            End If

            ' ===== 合成抗生素 =====
            If cell.HasFunction(GeneOntology.SynthesizeAntibiotic) Then
                Dim acetate = cell.GetMoleculeAmount(MoleculeType.Acetate)
                Dim gluAmino = cell.GetMoleculeAmount(MoleculeType.AminoMixGluFamily)
                If acetate >= 2 AndAlso gluAmino >= 1 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.Acetate, -2)
                        cell.AddMoleculeInternal(MoleculeType.AminoMixGluFamily, -1)
                        cell.AddMoleculeInternal(MoleculeType.Antibiotic, 1)
                    End If
                End If
            End If

            ' ===== 降解抗生素 =====
            If cell.HasFunction(GeneOntology.DegradeAntibiotic) Then
                Dim antibiotic = cell.GetMoleculeAmount(MoleculeType.Antibiotic)
                If antibiotic > 0 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.Antibiotic, -1)
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, 1)
                    End If
                End If
            End If

            ' ===== 细胞壁合成 =====
            If cell.HasFunction(GeneOntology.CellWallSynthesis) AndAlso Not cell.HasCellWall Then
                Dim nitrogen = cell.GetMoleculeAmount(MoleculeType.NitrogenSource)
                Dim aspAmino = cell.GetMoleculeAmount(MoleculeType.AminoMixAspFamily)
                If nitrogen >= 5 AndAlso aspAmino >= 3 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.NitrogenSource, -5)
                        cell.AddMoleculeInternal(MoleculeType.AminoMixAspFamily, -3)
                        cell.HasCellWall = True
                    End If
                End If
            End If

            ' ===== 铁载体合成 =====
            If cell.HasFunction(GeneOntology.SiderophoreSynthesis) Then
                Dim carbon = cell.GetMoleculeAmount(MoleculeType.CarbonSource)
                Dim nitrogen = cell.GetMoleculeAmount(MoleculeType.NitrogenSource)
                If carbon >= 2 AndAlso nitrogen >= 1 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, -2)
                        cell.AddMoleculeInternal(MoleculeType.NitrogenSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.Siderophore, 2)
                    End If
                End If
            End If
        End Sub
    End Class
End Namespace
