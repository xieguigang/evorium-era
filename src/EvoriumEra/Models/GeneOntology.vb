Namespace Models

    ''' <summary>
    ''' 功能词条：基因所能够行使的功能，目前我将功能设定为下面的枚举类型：
    ''' 
    ''' 降解大分子，核酸合成，复制DNA，细胞分裂，需氧能量代谢合成ATP，厌氧能量代谢合成ATP，合成抗生素，降解抗生素，基因转录，蛋白质翻译，细胞鞭毛运动，物质内吞，物质分泌，DNA整合，核酸降解，蛋白质降解，酸代谢，碱代谢，次级天然产物合成，固碳途径，群体感应，信号分子合成，细胞壁合成，生物膜合成，AMINO_MIX_GLU_FAMILY酶，AMINO_MIX_ASP_FAMILY酶，AMINO_MIX_SER_GLY酶，glucose转化酶，pyruvate酶，acetate酶
    ''' </summary>
    Public Enum GeneOntology
        DegradeMacromolecule
        NucleicAcidSynthesis
        ReplicateDNA
        CellDivision
        AerobicEnergyMetabolismATP
        AnaerobicEnergyMetabolismATP
        SynthesizeAntibiotic
        DegradeAntibiotic
        GeneTranscription
        ProteinTranslation
        FlagellarMovement
        Endocytosis
        Exocytosis
        DNAIntegration
        NucleicAcidDegradation
        ProteinDegradation
        AcidMetabolism
        BaseMetabolism
        SecondaryMetaboliteSynthesis
        CarbonFixation
        QuorumSensing
        SignalMoleculeSynthesis
        CellWallSynthesis
        BiofilmSynthesis
        AminoMixGluFamilyEnzyme
        AminoMixAspFamilyEnzyme
        AminoMixSerGlyEnzyme
        GlucoseConversionEnzyme
        PyruvateEnzyme
        AcetateEnzyme
    End Enum
End Namespace