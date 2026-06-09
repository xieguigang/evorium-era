Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v3.0 核心] 扩展代谢通路规则
    ''' 
    ''' 在v2.0基础代谢链（glucose→pyruvate→acetate→ATP）之上，
    ''' 增加更多代谢路径以丰富交叉喂养网络：
    ''' 
    ''' 碳代谢扩展：
    '''   pyruvate → succinate（琥珀酸途径）
    '''   pyruvate → ethanol（乙醇发酵）
    '''   acetate → butyrate（丁酸发酵）
    '''   formate → CO2 + H2（甲酸代谢）
    '''   fatty acid → acetyl-CoA → acetate（脂肪酸β氧化）
    ''' 
    ''' 硫代谢：
    '''   sulfate → sulfide（硫酸盐还原，厌氧呼吸）
    ''' 
    ''' 碳固定扩展：
    '''   CO2 → formate（厌氧碳固定）
    ''' 
    ''' 产甲烷：
    '''   acetate + CO2 → methane（产甲烷作用）
    ''' 
    ''' 多聚磷酸盐代谢：
    '''   phosphate + ATP → polyphosphate（储能）
    '''   polyphosphate → phosphate + ATP（释能）
    ''' </summary>
    Public Class ExtendedMetabolicPathwayRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(
                GeneOntology.SuccinateEnzyme,
                GeneOntology.EthanolMetabolism,
                GeneOntology.FormateMetabolism,
                GeneOntology.ButyrateEnzyme,
                GeneOntology.Methanogenesis,
                GeneOntology.SulfateReduction,
                GeneOntology.FattyAcidMetabolism,
                GeneOntology.PolyphosphateKinase
            )
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            ' ===== pyruvate → succinate =====
            ' 消耗2个pyruvate → 1个succinate + 3个ATP
            If cell.HasFunction(GeneOntology.SuccinateEnzyme) Then
                Dim pyruvate = cell.GetMoleculeAmount(MoleculeType.Pyruvate)
                If pyruvate >= 2 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.Pyruvate, -2)
                        cell.AddMoleculeInternal(MoleculeType.Succinate, 1)
                        cell.ATP = Math.Min(cell.ATP + 3, 1000)
                        env.AddMolecule(cell, MoleculeType.CarbonDioxide, 2)
                    End If
                End If
            End If

            ' ===== pyruvate → ethanol（乙醇发酵）=====
            ' 消耗1个pyruvate → 1个ethanol + 2个ATP + 1个CO2
            If cell.HasFunction(GeneOntology.EthanolMetabolism) Then
                Dim pyruvate = cell.GetMoleculeAmount(MoleculeType.Pyruvate)
                Dim oxygen = cell.GetMoleculeAmount(MoleculeType.Oxygen)
                If pyruvate > 0 AndAlso oxygen < 10 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.Pyruvate, -1)
                        cell.AddMoleculeInternal(MoleculeType.Ethanol, 1)
                        cell.ATP = Math.Min(cell.ATP + 2, 1000)
                        env.AddMolecule(cell, MoleculeType.CarbonDioxide, 1)
                    End If
                End If
            End If

            ' ===== acetate → butyrate（丁酸发酵）=====
            ' 消耗2个acetate → 1个butyrate + 3个ATP
            If cell.HasFunction(GeneOntology.ButyrateEnzyme) Then
                Dim acetate = cell.GetMoleculeAmount(MoleculeType.Acetate)
                If acetate >= 2 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.Acetate, -2)
                        cell.AddMoleculeInternal(MoleculeType.Butyrate, 1)
                        cell.ATP = Math.Min(cell.ATP + 3, 1000)
                        env.AddMolecule(cell, MoleculeType.HydrogenIon, 2)
                    End If
                End If
            End If

            ' ===== formate → CO2 + H2（甲酸代谢）=====
            ' 消耗1个formate → 2个ATP + 1个CO2
            If cell.HasFunction(GeneOntology.FormateMetabolism) Then
                Dim formate = cell.GetMoleculeAmount(MoleculeType.Formate)
                If formate > 0 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.Formate, -1)
                        cell.ATP = Math.Min(cell.ATP + 2, 1000)
                        env.AddMolecule(cell, MoleculeType.CarbonDioxide, 1)
                        env.AddMolecule(cell, MoleculeType.HydrogenIon, 1)
                    End If
                End If
            End If

            ' ===== acetate → methane（产甲烷作用）=====
            ' 消耗1个acetate → 1个methane + 4个ATP（严格厌氧）
            If cell.HasFunction(GeneOntology.Methanogenesis) Then
                Dim acetate = cell.GetMoleculeAmount(MoleculeType.Acetate)
                Dim oxygen = cell.GetMoleculeAmount(MoleculeType.Oxygen)
                If acetate > 0 AndAlso oxygen < 3 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.Acetate, -1)
                        cell.AddMoleculeInternal(MoleculeType.Methane, 1)
                        cell.ATP = Math.Min(cell.ATP + 4, 1000)
                        env.AddMolecule(cell, MoleculeType.CarbonDioxide, 1)
                    End If
                End If
            End If

            ' ===== sulfate → sulfide（硫酸盐还原，厌氧呼吸）=====
            ' 消耗1个sulfate + 1个acetate → 1个sulfide + 5个ATP
            If cell.HasFunction(GeneOntology.SulfateReduction) Then
                Dim sulfate = cell.GetMoleculeAmount(MoleculeType.Sulfate)
                Dim acetate = cell.GetMoleculeAmount(MoleculeType.Acetate)
                Dim oxygen = cell.GetMoleculeAmount(MoleculeType.Oxygen)
                If sulfate > 0 AndAlso acetate > 0 AndAlso oxygen < 5 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.Sulfate, -1)
                        cell.AddMoleculeInternal(MoleculeType.Acetate, -1)
                        cell.AddMoleculeInternal(MoleculeType.Sulfide, 1)
                        cell.ATP = Math.Min(cell.ATP + 5, 1000)
                        env.AddMolecule(cell, MoleculeType.CarbonDioxide, 2)
                    End If
                End If
            End If

            ' ===== fatty acid → acetate（脂肪酸β氧化）=====
            ' 消耗1个fatty acid → 2个acetate + 5个ATP
            If cell.HasFunction(GeneOntology.FattyAcidMetabolism) Then
                Dim fa = cell.GetMoleculeAmount(MoleculeType.FattyAcid)
                If fa > 0 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.FattyAcid, -1)
                        cell.AddMoleculeInternal(MoleculeType.Acetate, 2)
                        cell.ATP = Math.Min(cell.ATP + 5, 1000)
                        env.AddMolecule(cell, MoleculeType.CarbonDioxide, 2)
                    End If
                End If
            End If

            ' ===== 多聚磷酸盐代谢 =====
            If cell.HasFunction(GeneOntology.PolyphosphateKinase) Then
                Dim phosphate = cell.GetMoleculeAmount(MoleculeType.Phosphate)
                Dim polyP = cell.GetMoleculeAmount(MoleculeType.Polyphosphate)

                ' ATP充足时：储存磷酸盐
                If cell.ATP > 500 AndAlso phosphate >= 5 Then
                    cell.ATP -= 3
                    cell.AddMoleculeInternal(MoleculeType.Phosphate, -5)
                    cell.AddMoleculeInternal(MoleculeType.Polyphosphate, 1)
                End If

                ' ATP不足时：释放磷酸盐
                If cell.ATP < 200 AndAlso polyP > 0 Then
                    cell.AddMoleculeInternal(MoleculeType.Polyphosphate, -1)
                    cell.AddMoleculeInternal(MoleculeType.Phosphate, 5)
                    cell.ATP = Math.Min(cell.ATP + 3, 1000)
                End If
            End If
        End Sub
    End Class
End Namespace
