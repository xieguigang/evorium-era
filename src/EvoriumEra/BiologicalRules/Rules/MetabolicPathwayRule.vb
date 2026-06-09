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

        ReadOnly reactions As Reaction()

        Sub New()
            Call MyBase.New(
                GeneOntology.GlucoseConversionEnzyme,
                GeneOntology.PyruvateEnzyme,
                GeneOntology.AcetateEnzyme,
                GeneOntology.LactateDehydrogenase,
                GeneOntology.NucleicAcidSynthesis
            )

            ' ===== Step 1: glucose → pyruvate =====
            ' 消耗1个glucose + 1个水 → 2个pyruvate + 2个ATP
            ' 需要GlucoseConversionEnzyme蛋白质
            Dim glucoseConversion = New Reaction(GeneOntology.GlucoseConversionEnzyme, 2, exemptATP:=True).Left((MoleculeType.Glucose, -1), (MoleculeType.Water, -1)).Right((MoleculeType.Pyruvate, 2), (MoleculeType.CarbonDioxide, 1)) ' 糖酵解产生少量CO2
            ' ===== Step 2: pyruvate → acetate =====
            ' 消耗1个pyruvate → 1个acetate + 1个CO2 + 1个ATP
            ' 需要PyruvateEnzyme蛋白质
            Dim pyruvateMetabolism = New Reaction(GeneOntology.PyruvateEnzyme, 1, exemptATP:=False).Left((MoleculeType.Pyruvate, -1)).Right((MoleculeType.Acetate, 1), (MoleculeType.CarbonDioxide, 1))
            ' ===== Step 3: acetate → ATP (厌氧) =====
            ' 消耗1个acetate → 5个ATP + 2个CO2 + 2个H+
            ' 需要AcetateEnzyme蛋白质，有氧时抑制
            Dim acetateMetabolism = New Reaction(GeneOntology.AcetateEnzyme, 5, exemptATP:=True).Left((MoleculeType.Acetate, -1)).Right((MoleculeType.CarbonDioxide, 2), (MoleculeType.HydrogenIon, 2)).Inhibit((MoleculeType.Oxygen, 10))
            ' ===== Step 4: pyruvate → lactate (发酵) =====
            ' 消耗1个pyruvate → 1个lactate + 2个ATP
            ' 需要LactateDehydrogenase蛋白质，无氧时优先
            Dim lactateDehydro = New Reaction(GeneOntology.LactateDehydrogenase, 2, exemptATP:=True).Left((MoleculeType.Pyruvate, -1)).Right((MoleculeType.Lactate, 1)).Inhibit((MoleculeType.Oxygen, 10))
            Dim nucleotideSyns = New Reaction(GeneOntology.NucleicAcidSynthesis, 0, exemptATP:=False).Left((MoleculeType.Pyruvate, -1), (MoleculeType.NitrogenSource, -1)).Right((MoleculeType.Nucleotide, 1))

            reactions = {glucoseConversion, pyruvateMetabolism, acetateMetabolism, lactateDehydro, nucleotideSyns}
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            For Each rxn As Reaction In reactions
                Call rxn.Execute(cell)
            Next
        End Sub
    End Class
End Namespace
