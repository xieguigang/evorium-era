Namespace Models

    ''' <summary>
    ''' 功能词条：基因所能够行使的功能，目前我将功能设定为下面的枚举类型：
    ''' 
    ''' 降解大分子，核酸合成，复制DNA，细胞分裂，需氧能量代谢合成ATP，厌氧能量代谢合成ATP，合成抗生素，降解抗生素，基因转录，蛋白质翻译，细胞鞭毛运动，物质内吞，物质分泌，DNA整合，核酸降解，蛋白质降解，酸代谢，碱代谢，次级天然产物合成，固碳途径，群体感应，信号分子合成，细胞壁合成，生物膜合成，AMINO_MIX_GLU_FAMILY酶，AMINO_MIX_ASP_FAMILY酶，AMINO_MIX_SER_GLY酶，glucose转化酶，pyruvate酶，acetate酶
    ''' </summary>
    Public Enum GeneOntology
        DegradeMacromolecule               ' 降解大分子
        NucleicAcidSynthesis               ' 核酸合成
        ReplicateDNA                       ' DNA复制
        CellDivision                       ' 细胞分裂
        AerobicEnergyMetabolismATP         ' 有氧能量代谢产ATP
        AnaerobicEnergyMetabolismATP       ' 无氧能量代谢产ATP
        SynthesizeAntibiotic               ' 合成抗生素
        DegradeAntibiotic                  ' 降解抗生素
        GeneTranscription                  ' 基因转录
        ProteinTranslation                 ' 蛋白质翻译
        FlagellarMovement                  ' 鞭毛运动
        Endocytosis                        ' 内吞作用
        Exocytosis                         ' 胞吐作用/外排作用
        DNAIntegration                     ' DNA整合
        NucleicAcidDegradation             ' 核酸降解
        ProteinDegradation                 ' 蛋白质降解
        AcidMetabolism                     ' 酸代谢
        BaseMetabolism                     ' 碱代谢
        SecondaryMetaboliteSynthesis       ' 次级代谢产物合成
        CarbonFixation                     ' 碳固定/固碳作用
        QuorumSensing                      ' 群体感应
        SignalMoleculeSynthesis            ' 信号分子合成
        CellWallSynthesis                  ' 细胞壁合成
        BiofilmSynthesis                   ' 生物膜合成
        AminoMixGluFamilyEnzyme            ' 谷氨酸族氨基酸合成酶
        AminoMixAspFamilyEnzyme            ' 天冬氨酸族氨基酸合成酶
        AminoMixSerGlyEnzyme               ' 丝氨酸/甘氨酸族氨基酸合成酶
        GlucoseConversionEnzyme            ' 葡萄糖转化酶
        PyruvateEnzyme                     ' 丙酮酸酶/丙酮酸代谢酶
        AcetateEnzyme                      ' 乙酸酶/乙酸代谢酶
    End Enum
End Namespace