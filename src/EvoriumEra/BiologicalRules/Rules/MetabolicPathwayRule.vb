Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v2.0 核心] 代谢通路规则
    ''' 
    ''' 实现完整的代谢链：
    '''   glucose → pyruvate → acetate → ATP (厌氧)
    '''   glucose + O2 → ATP (好氧)
    '''   pyruvate → lactate (发酵)
    ''' 
    ''' 每一步需要对应的酶基因已转录为蛋白质才能执行。
    ''' 这是交叉喂养网络的核心驱动力。
    ''' </summary>
    Public Class MetabolicPathwayRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(
                GeneOntology.GlucoseConversionEnzyme,
                GeneOntology.PyruvateEnzyme,
                GeneOntology.AcetateEnzyme,
                GeneOntology.LactateDehydrogenase
            )
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            ' ===== Step 1: glucose → pyruvate =====
            ' 消耗1个glucose + 1个水 → 2个pyruvate + 2个ATP
            ' 需要GlucoseConversionEnzyme蛋白质
            If cell.HasFunction(GeneOntology.GlucoseConversionEnzyme) Then
                Dim glucose = cell.GetMoleculeAmount(MoleculeType.Glucose)
                Dim water = cell.GetMoleculeAmount(MoleculeType.Water)

                If glucose > 0 AndAlso water > 0 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.Glucose, -1)
                        cell.AddMoleculeInternal(MoleculeType.Water, -1)
                        cell.AddMoleculeInternal(MoleculeType.Pyruvate, 2)
                        cell.ATP = Math.Min(cell.ATP + 2, 1000)
                        ' 糖酵解产生少量CO2
                        cell.AddMoleculeInternal(MoleculeType.CarbonDioxide, 1)
                    End If
                End If
            End If

            ' ===== Step 2: pyruvate → acetate =====
            ' 消耗1个pyruvate → 1个acetate + 1个CO2 + 1个ATP
            ' 需要PyruvateEnzyme蛋白质
            If cell.HasFunction(GeneOntology.PyruvateEnzyme) Then
                Dim pyruvate = cell.GetMoleculeAmount(MoleculeType.Pyruvate)

                If pyruvate > 0 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.Pyruvate, -1)
                        cell.AddMoleculeInternal(MoleculeType.Acetate, 1)
                        cell.AddMoleculeInternal(MoleculeType.CarbonDioxide, 1)
                        cell.ATP = Math.Min(cell.ATP + 1, 1000)
                    End If
                End If
            End If

            ' ===== Step 3: acetate → ATP (厌氧) =====
            ' 消耗1个acetate → 5个ATP + 2个CO2 + 2个H+
            ' 需要AcetateEnzyme蛋白质，有氧时抑制
            If cell.HasFunction(GeneOntology.AcetateEnzyme) Then
                Dim acetate = cell.GetMoleculeAmount(MoleculeType.Acetate)
                Dim oxygen = cell.GetMoleculeAmount(MoleculeType.Oxygen)

                If acetate > 0 AndAlso oxygen < 10 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.Acetate, -1)
                        cell.ATP = Math.Min(cell.ATP + 5, 1000)
                        cell.AddMoleculeInternal(MoleculeType.CarbonDioxide, 2)
                        cell.AddMoleculeInternal(MoleculeType.HydrogenIon, 2)
                    End If
                End If
            End If

            ' ===== Step 4: pyruvate → lactate (发酵) =====
            ' 消耗1个pyruvate → 1个lactate + 2个ATP
            ' 需要LactateDehydrogenase蛋白质，无氧时优先
            If cell.HasFunction(GeneOntology.LactateDehydrogenase) Then
                Dim pyruvate = cell.GetMoleculeAmount(MoleculeType.Pyruvate)
                Dim oxygen = cell.GetMoleculeAmount(MoleculeType.Oxygen)

                If pyruvate > 0 AndAlso oxygen < 10 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.Pyruvate, -1)
                        cell.AddMoleculeInternal(MoleculeType.Lactate, 1)
                        cell.ATP = Math.Min(cell.ATP + 2, 1000)
                    End If
                End If
            End If
        End Sub
    End Class
End Namespace
