Namespace Models

    ''' <summary>
    ''' 代谢物：细胞生命活动的物质基础
    ''' v2.0新增：Lactate（乳酸）, Siderophore（铁载体）
    ''' </summary>
    Public Enum MoleculeType
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
        Glucose
        Pyruvate
        Acetate
        AminoMixGluFamily
        AminoMixAspFamily
        AminoMixSerGly
        Antibiotic
        SecondaryMetabolite
        SignalMolecule
        CellWall
        Biofilm
        Lactate         ' [v2.0] 乳酸
        Siderophore     ' [v2.0] 铁载体
    End Enum
End Namespace
