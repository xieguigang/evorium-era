Namespace Models

    ''' <summary>
    ''' 代谢物： 细胞生命活动的物质基础，包括：水，氧气，二氧化碳，氢离子，氢氧根离子，碳酸根离子，ATP，核苷酸，DNA，碳源，氮源，glucose，pyruvate，acetate，AMINO_MIX_GLU_FAMILY氨基酸，AMINO_MIX_ASP_FAMILY氨基酸，AMINO_MIX_SER_GLY氨基酸，抗生素，次级天然产物，信号分子，细胞壁，生物膜 这几种分子实体
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
        ''' <summary>
        ''' AMINO_MIX_GLU_FAMILY氨基酸
        ''' </summary>
        AminoMixGluFamily
        ''' <summary>
        ''' AMINO_MIX_ASP_FAMILY氨基酸
        ''' </summary>
        AminoMixAspFamily
        ''' <summary>
        ''' AMINO_MIX_SER_GLY氨基酸
        ''' </summary>
        AminoMixSerGly
        Antibiotic
        SecondaryMetabolite
        SignalMolecule
        CellWall
        Biofilm
    End Enum
End Namespace