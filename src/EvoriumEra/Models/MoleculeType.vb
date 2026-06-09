Namespace Models

    ''' <summary>
    ''' 代谢物：细胞生命活动的物质基础
    ''' 
    ''' v2.0 新增：Lactate（乳酸）, Siderophore（铁载体）
    ''' v3.0 大幅扩展：
    '''   - 温度相关：Heat（热量/温度载体）
    '''   - 渗透压相关：SodiumIon, PotassiumIon, ChlorideIon, CalciumIon, 
    '''     CompatibleSolute（相容溶质/甜菜碱）, MagnesiumIon
    '''   - 扩展碳代谢：Succinate, Ethanol, Formate, Butyrate, FattyAcid, Methane
    '''   - 扩展硫/铁：Sulfate, Sulfide, IronII, IronIII
    '''   - 扩展磷：Phosphate, Polyphosphate
    '''   - 扩展氨基酸：AminoMixAromatic, AminoMixBranched, AminoMixThiol
    '''   - 扩展次级代谢：Vitamin, Pigment, Toxin
    ''' </summary>
    Public Enum MoleculeType
        ' ===== 基础分子 =====
        Water
        Oxygen
        CarbonDioxide
        HydrogenIon
        HydroxideIon
        CarbonateIon
        ATP
        Nucleotide
        DNA
        CarbonSource
        NitrogenSource

        ' ===== 核心碳代谢 =====
        Glucose
        Pyruvate
        Acetate
        Lactate              ' [v2.0] 乳酸
        Succinate            ' [v3] 琥珀酸（TCA中间体）
        Ethanol              ' [v3] 乙醇
        Formate              ' [v3] 甲酸
        Butyrate             ' [v3] 丁酸
        FattyAcid            ' [v3] 脂肪酸
        Methane              ' [v3] 甲烷

        ' ===== 氨基酸 =====
        AminoMixGluFamily
        AminoMixAspFamily
        AminoMixSerGly
        AminoMixAromatic     ' [v3] 芳香族氨基酸（苯丙/酪/色）
        AminoMixBranched     ' [v3] 支链氨基酸（亮/异亮/缬）
        AminoMixThiol        ' [v3] 含硫氨基酸（半胱/蛋）

        ' ===== 离子与矿物质 =====
        Phosphate            ' [v3] 磷酸盐
        Polyphosphate        ' [v3] 多聚磷酸（储能）
        Sulfate              ' [v3] 硫酸盐
        Sulfide              ' [v3] 硫化物
        IronII               ' [v3] Fe2+
        IronIII              ' [v3] Fe3+
        SodiumIon            ' [v3] Na+
        PotassiumIon         ' [v3] K+
        ChlorideIon          ' [v3] Cl-
        CalciumIon           ' [v3] Ca2+
        MagnesiumIon         ' [v3] Mg2+

        ' ===== 渗透压调节 =====
        CompatibleSolute     ' [v3] 相容溶质（甜菜碱/肌醇等）

        ' ===== 信号/防御 =====
        Antibiotic
        SecondaryMetabolite
        SignalMolecule
        Siderophore          ' [v2.0] 铁载体
        Vitamin              ' [v3] 维生素
        Pigment              ' [v3] 色素
        Toxin                ' [v3] 毒素

        ' ===== 结构 =====
        CellWall
        Biofilm

        ' ===== 温度 =====
        Heat                 ' [v3] 热量（温度载体，用于热扩散计算）
    End Enum
End Namespace
