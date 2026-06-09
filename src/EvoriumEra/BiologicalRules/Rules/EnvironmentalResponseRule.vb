Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v2.0 改进] 环境响应规则
    ''' 
    ''' 包含：酸代谢、碱代谢、抗生素抑制
    ''' </summary>
    Public Class EnvironmentalResponseRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(GeneOntology.AcidMetabolism, GeneOntology.BaseMetabolism)
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            ' 酸代谢：消耗H+和碳源，产生水，+1 ATP
            If cell.HasFunction(GeneOntology.AcidMetabolism) Then
                Dim hIon = cell.GetMoleculeAmount(MoleculeType.HydrogenIon)
                Dim carbon = cell.GetMoleculeAmount(MoleculeType.CarbonSource)
                If hIon >= 20 AndAlso carbon > 0 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.HydrogenIon, -1)
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.Water, 1)
                        cell.ATP = Math.Min(cell.ATP + 1, 1000)
                    End If
                End If
            End If

            ' 碱代谢：消耗OH-和碳源，产生水，+1 ATP
            If cell.HasFunction(GeneOntology.BaseMetabolism) Then
                Dim ohIon = cell.GetMoleculeAmount(MoleculeType.HydroxideIon)
                Dim carbon = cell.GetMoleculeAmount(MoleculeType.CarbonSource)
                If ohIon >= 20 AndAlso carbon > 0 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.HydroxideIon, -1)
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.Water, 1)
                        cell.ATP = Math.Min(cell.ATP + 1, 1000)
                    End If
                End If
            End If

            ' 抗生素抑制：有抗生素时额外消耗ATP
            If cell.GetMoleculeAmount(MoleculeType.Antibiotic) > 0 Then
                cell.ATP = Math.Max(0, cell.ATP - 1)
            End If
        End Sub
    End Class
End Namespace
