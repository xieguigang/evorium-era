Namespace Models

    ''' <summary>
    ''' 功能词条：基因所能够行使的功能
    ''' 
    ''' v2.0新增：LactateDehydrogenase（乳酸脱氢酶）, SiderophoreSynthesis（铁载体合成）
    ''' v3.0新增：
    '''   - Thermotolerance（耐热蛋白）：保护其他蛋白在高温下不失活
    '''   - ColdShockResponse（冷休克响应）：低温下维持蛋白活性
    '''   - Osmoregulation（渗透压调节）：调节胞内离子强度
    '''   - CompatibleSoluteSynthesis（相容溶质合成）：合成甜菜碱等渗透保护物质
    '''   - FattyAcidMetabolism（脂肪酸代谢）：脂肪酸β氧化
    '''   - SuccinateEnzyme（琥珀酸代谢酶）：TCA中间体
    '''   - EthanolMetabolism（乙醇代谢酶）
    '''   - FormateMetabolism（甲酸代谢酶）
    '''   - ButyrateEnzyme（丁酸代谢酶）
    '''   - Methanogenesis（产甲烷作用）
    '''   - SulfateReduction（硫酸盐还原）
    '''   - IronRedox（铁氧化还原）
    '''   - PolyphosphateKinase（多聚磷酸激酶）
    '''   - PhosphateTransport（磷酸盐转运）
    '''   - AminoMixAromaticEnzyme（芳香族氨基酸合成酶）
    '''   - AminoMixBranchedEnzyme（支链氨基酸合成酶）
    '''   - AminoMixThiolEnzyme（含硫氨基酸合成酶）
    '''   - VitaminSynthesis（维生素合成）
    '''   - PigmentSynthesis（色素合成）
    '''   - ToxinSynthesis（毒素合成）
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

        ' ===== v3.0 温度相关 =====
        Thermotolerance                    ' [v3] 耐热蛋白：保护其他蛋白高温不失活
        ColdShockResponse                  ' [v3] 冷休克响应：低温下维持蛋白活性

        ' ===== v3.0 渗透压相关 =====
        Osmoregulation                     ' [v3] 渗透压调节：主动排出/积累离子
        CompatibleSoluteSynthesis          ' [v3] 相容溶质合成：合成甜菜碱等渗透保护物质

        ' ===== v3.0 扩展代谢网络 =====
        FattyAcidMetabolism                ' [v3] 脂肪酸β氧化
        SuccinateEnzyme                    ' [v3] 琥珀酸代谢酶
        EthanolMetabolism                  ' [v3] 乙醇代谢酶
        FormateMetabolism                  ' [v3] 甲酸代谢酶
        ButyrateEnzyme                     ' [v3] 丁酸代谢酶
        Methanogenesis                     ' [v3] 产甲烷作用
        SulfateReduction                   ' [v3] 硫酸盐还原
        IronRedox                          ' [v3] 铁氧化还原

        ' ===== v3.0 扩展离子/矿物质 =====
        PolyphosphateKinase                ' [v3] 多聚磷酸激酶：储存/释放磷酸盐
        PhosphateTransport                 ' [v3] 磷酸盐转运

        ' ===== v3.0 扩展氨基酸 =====
        AminoMixAromaticEnzyme             ' [v3] 芳香族氨基酸合成酶（苯丙/酪/色）
        AminoMixBranchedEnzyme             ' [v3] 支链氨基酸合成酶（亮/异亮/缬）
        AminoMixThiolEnzyme                ' [v3] 含硫氨基酸合成酶（半胱/蛋）

        ' ===== v3.0 扩展次级代谢 =====
        VitaminSynthesis                   ' [v3] 维生素合成
        PigmentSynthesis                   ' [v3] 色素合成
        ToxinSynthesis                     ' [v3] 毒素合成
    End Enum
End Namespace
