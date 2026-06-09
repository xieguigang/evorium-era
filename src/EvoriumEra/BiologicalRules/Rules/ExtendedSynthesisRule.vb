Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v3.0] 扩展合成规则
    ''' 
    ''' 包含v3.0新增的合成功能：
    ''' - 芳香族氨基酸合成（苯丙/酪/色）
    ''' - 支链氨基酸合成（亮/异亮/缬）
    ''' - 含硫氨基酸合成（半胱/蛋）
    ''' - 维生素合成
    ''' - 色素合成
    ''' - 毒素合成
    ''' - 固碳扩展：CO2 → formate
    ''' </summary>
    Public Class ExtendedSynthesisRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(
                GeneOntology.AminoMixAromaticEnzyme,
                GeneOntology.AminoMixBranchedEnzyme,
                GeneOntology.AminoMixThiolEnzyme,
                GeneOntology.VitaminSynthesis,
                GeneOntology.PigmentSynthesis,
                GeneOntology.ToxinSynthesis
            )
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            ' ===== 芳香族氨基酸合成 =====
            ' 消耗3个碳源 + 1个氮源 + 1个磷酸盐 → 3个芳香族氨基酸
            If cell.HasFunction(GeneOntology.AminoMixAromaticEnzyme) Then
                Dim carbon = cell.GetMoleculeAmount(MoleculeType.CarbonSource)
                Dim nitrogen = cell.GetMoleculeAmount(MoleculeType.NitrogenSource)
                Dim phosphate = cell.GetMoleculeAmount(MoleculeType.Phosphate)
                If carbon >= 3 AndAlso nitrogen >= 1 AndAlso phosphate >= 1 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, -3)
                        cell.AddMoleculeInternal(MoleculeType.NitrogenSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.Phosphate, -1)
                        cell.AddMoleculeInternal(MoleculeType.AminoMixAromatic, 3)
                    End If
                End If
            End If

            ' ===== 支链氨基酸合成 =====
            ' 消耗2个碳源 + 1个氮源 + 1个pyruvate → 3个支链氨基酸
            If cell.HasFunction(GeneOntology.AminoMixBranchedEnzyme) Then
                Dim carbon = cell.GetMoleculeAmount(MoleculeType.CarbonSource)
                Dim nitrogen = cell.GetMoleculeAmount(MoleculeType.NitrogenSource)
                Dim pyruvate = cell.GetMoleculeAmount(MoleculeType.Pyruvate)
                If carbon >= 2 AndAlso nitrogen >= 1 AndAlso pyruvate >= 1 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, -2)
                        cell.AddMoleculeInternal(MoleculeType.NitrogenSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.Pyruvate, -1)
                        cell.AddMoleculeInternal(MoleculeType.AminoMixBranched, 3)
                    End If
                End If
            End If

            ' ===== 含硫氨基酸合成 =====
            ' 消耗1个碳源 + 1个氮源 + 1个sulfate → 2个含硫氨基酸
            If cell.HasFunction(GeneOntology.AminoMixThiolEnzyme) Then
                Dim carbon = cell.GetMoleculeAmount(MoleculeType.CarbonSource)
                Dim nitrogen = cell.GetMoleculeAmount(MoleculeType.NitrogenSource)
                Dim sulfate = cell.GetMoleculeAmount(MoleculeType.Sulfate)
                If carbon >= 1 AndAlso nitrogen >= 1 AndAlso sulfate >= 1 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.NitrogenSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.Sulfate, -1)
                        cell.AddMoleculeInternal(MoleculeType.AminoMixThiol, 2)
                    End If
                End If
            End If

            ' ===== 维生素合成 =====
            ' 消耗2个碳源 + 1个氮源 + 1个AMINO_MIX_GLU → 1个维生素
            ' 维生素可分泌到环境，被其他细胞摄取后提升代谢效率
            If cell.HasFunction(GeneOntology.VitaminSynthesis) Then
                Dim carbon = cell.GetMoleculeAmount(MoleculeType.CarbonSource)
                Dim nitrogen = cell.GetMoleculeAmount(MoleculeType.NitrogenSource)
                Dim gluAA = cell.GetMoleculeAmount(MoleculeType.AminoMixGluFamily)
                If carbon >= 2 AndAlso nitrogen >= 1 AndAlso gluAA >= 1 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, -2)
                        cell.AddMoleculeInternal(MoleculeType.NitrogenSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.AminoMixGluFamily, -1)
                        cell.AddMoleculeInternal(MoleculeType.Vitamin, 1)
                    End If
                End If
            End If

            ' ===== 色素合成 =====
            ' 消耗1个AMINO_MIX_AROMATIC + 1个碳源 → 1个色素
            ' 色素可保护细胞免受UV/高温损伤
            If cell.HasFunction(GeneOntology.PigmentSynthesis) Then
                Dim aromatic = cell.GetMoleculeAmount(MoleculeType.AminoMixAromatic)
                Dim carbon = cell.GetMoleculeAmount(MoleculeType.CarbonSource)
                If aromatic >= 1 AndAlso carbon >= 1 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.AminoMixAromatic, -1)
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.Pigment, 1)
                    End If
                End If
            End If

            ' ===== 毒素合成 =====
            ' 消耗2个AMINO_MIX_THIOL + 1个acetate → 1个毒素
            ' 毒素比抗生素更强，但合成成本更高
            If cell.HasFunction(GeneOntology.ToxinSynthesis) Then
                Dim thiol = cell.GetMoleculeAmount(MoleculeType.AminoMixThiol)
                Dim acetate = cell.GetMoleculeAmount(MoleculeType.Acetate)
                If thiol >= 2 AndAlso acetate >= 1 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.AminoMixThiol, -2)
                        cell.AddMoleculeInternal(MoleculeType.Acetate, -1)
                        cell.AddMoleculeInternal(MoleculeType.Toxin, 1)
                    End If
                End If
            End If
        End Sub
    End Class
End Namespace
