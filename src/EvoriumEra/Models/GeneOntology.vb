Namespace Models

    ''' <summary>
    ''' 功能词条：基因所能够行使的功能
    ''' v2.0新增：LactateDehydrogenase（乳酸脱氢酶）, SiderophoreSynthesis（铁载体合成）
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
        Endocytosis                        ' 物质内吞
        Exocytosis                         ' 物质分泌
        DNAIntegration                     ' DNA整合
        NucleicAcidDegradation             ' 核酸降解
        ProteinDegradation                 ' 蛋白质降解
        AcidMetabolism                     ' 酸代谢
        BaseMetabolism                      ' 碱代谢
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
        LactateDehydrogenase               ' [v2.0] 乳酸脱氢酶
        SiderophoreSynthesis               ' [v2.0] 铁载体合成
    End Enum
End Namespace
