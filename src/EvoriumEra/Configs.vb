Public Class Configs

    Public Property gridW As Integer = 60
    Public Property gridH As Integer = 60
    Public Property gridD As Integer = 30
    Public Property SnapshotInterval As Integer = 1

    Public Property InitCellNumbers As Integer = 80
    Public Property MaxVoxelActions As Integer = 5
    Public Property MaxCellActions As Integer = 12

    Public Property MaxCellContentCapacity As Integer = 10000

    ' [v2.0] 新增配置参数

    ''' <summary>每迭代环境营养补充量（碳源）</summary>
    Public Property NutrientReplenishmentCarbon As Integer = 30

    ''' <summary>每迭代环境营养补充量（氮源）</summary>
    Public Property NutrientReplenishmentNitrogen As Integer = 15

    ''' <summary>营养热点数量</summary>
    Public Property NutrientHotspotCount As Integer = 5

    ''' <summary>营养热点补充强度</summary>
    Public Property NutrientHotspotStrength As Integer = 50

    ''' <summary>氧气梯度：表层氧气浓度</summary>
    Public Property SurfaceOxygenLevel As Integer = 200

    ''' <summary>氧气梯度：每层深度衰减量</summary>
    Public Property OxygenDecayPerLayer As Integer = 8

    ''' <summary>代谢溢流阈值（容量百分比）</summary>
    Public Property OverflowThreshold As Double = 0.6

    ''' <summary>代谢溢流分泌比例</summary>
    Public Property OverflowSecretionFraction As Double = 0.5

    ''' <summary>突变率</summary>
    Public Property MutationRate As Double = 0.03

    ''' <summary>基因重复突变概率</summary>
    Public Property GeneDuplicationRate As Double = 0.01

    ''' <summary>基因缺失突变概率</summary>
    Public Property GeneDeletionRate As Double = 0.01

    ''' <summary>细胞分裂最低ATP阈值</summary>
    Public Property DivisionMinATP As Integer = 300

    ''' <summary>细胞分裂最低分子总量</summary>
    Public Property DivisionMinTotalMolecules As Integer = 500

    ''' <summary>ATP上限</summary>
    Public Property MaxATP As Integer = 1000

    ''' <summary>连续无ATP死亡迭代数</summary>
    Public Property StarvationDeathIterations As Integer = 5

    ''' <summary>初始基因组最小基因数</summary>
    Public Property InitGenomeMinGenes As Integer = 5

    ''' <summary>初始基因组最大基因数</summary>
    Public Property InitGenomeMaxGenes As Integer = 15
    Public Property OxygenGradientDecay As Double = 10

    Public Shared Function [Default]() As Configs
        Return New Configs
    End Function

End Class
