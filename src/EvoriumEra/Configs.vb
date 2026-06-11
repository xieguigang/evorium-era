Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Serialization.JSON

Public Class Configs

    Public Property gridW As Integer = 60
    Public Property gridH As Integer = 60
    Public Property gridD As Integer = 30
    Public Property SnapshotInterval As Integer = 1

    Public Property InitCellNumbers As Integer = 5000
    Public Property InitCellPlasmidUpperBound As Integer = 2
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
    Public Property StarvationDeathIterations As Integer = 10

    ''' <summary>初始基因组最小基因数</summary>
    Public Property InitGenomeMinGenes As Integer = 6

    ''' <summary>初始基因组最大基因数</summary>
    Public Property InitGenomeMaxGenes As Integer = 20
    Public Property OxygenGradientDecay As Double = 10

    ' ===== v3.0 温度配置 =====

    ''' <summary>环境基础温度（°C），昼夜循环的中值</summary>
    Public Property BaseTemperature As Double = 25.0

    ''' <summary>昼夜温度振幅（°C），实际温度 = BaseTemp + Amplitude * sin(2π*t/Period)</summary>
    Public Property DiurnalTemperatureAmplitude As Double = 10.0

    ''' <summary>昼夜周期（迭代数），例如240=每240次迭代一个昼夜循环</summary>
    Public Property DiurnalPeriod As Integer = 240

    ''' <summary>温度深度衰减：每层深度降低的温度（°C），表层最暖</summary>
    Public Property TemperatureDepthDecay As Double = 0.3

    ''' <summary>蛋白质失活温度阈值（°C），超过此温度非耐热蛋白开始失活</summary>
    Public Property ProteinDenaturationTemp As Double = 45.0

    ''' <summary>蛋白质完全失活温度（°C），超过此温度非耐热蛋白全部失活</summary>
    Public Property ProteinCompleteDenaturationTemp As Double = 60.0

    ''' <summary>蛋白质活性降低的低温阈值（°C），低于此温度蛋白活性开始下降</summary>
    Public Property ColdShockTemp As Double = 15.0

    ''' <summary>蛋白质严重失活的低温阈值（°C）</summary>
    Public Property SevereColdShockTemp As Double = 5.0

    ''' <summary>代谢产热系数：每次ATP消耗产生的热量（°C/ATP）</summary>
    Public Property MetabolicHeatPerATP As Double = 0.02

    ''' <summary>热扩散系数：相邻格子间温度均衡速率（0.0-1.0）</summary>
    Public Property HeatDiffusionRate As Double = 0.1

    ''' <summary>环境温度向基线恢复的速率（0.0-1.0）</summary>
    Public Property TemperatureRecoveryRate As Double = 0.05

    ' ===== v3.0 渗透压配置 =====

    ''' <summary>等渗离子强度参考值（mM），高于此为高渗，低于此为低渗</summary>
    Public Property IsotonicIonStrength As Double = 300.0

    ''' <summary>高渗环境下每迭代水流失量（占胞内水的比例）</summary>
    Public Property HyperosmoticWaterLossRate As Double = 0.05

    ''' <summary>低渗环境下每迭代水流入量（占胞外水的比例）</summary>
    Public Property HypoosmoticWaterGainRate As Double = 0.03

    ''' <summary>渗透压失衡时ATP额外消耗（每迭代）</summary>
    Public Property OsmoticStressATPCost As Integer = 2

    ''' <summary>相容溶质合成消耗碳源数</summary>
    Public Property CompatibleSoluteCarbonCost As Integer = 3

    ''' <summary>相容溶质合成消耗氮源数</summary>
    Public Property CompatibleSoluteNitrogenCost As Integer = 1

    ' ===== v3.0 扩展代谢配置 =====

    ''' <summary>环境初始磷酸盐浓度</summary>
    Public Property InitialPhosphateLevel As Integer = 50

    ''' <summary>环境初始硫酸盐浓度</summary>
    Public Property InitialSulfateLevel As Integer = 30

    ''' <summary>环境初始铁离子浓度</summary>
    Public Property InitialIronLevel As Integer = 20

    ''' <summary>环境初始钠/钾/氯离子浓度</summary>
    Public Property InitialSaltIonLevel As Integer = 100

    ' ===== v4.0 胞外蛋白质活性配置 =====

    ''' <summary>胞外蛋白质保持活性的循环周期数（默认5）</summary>
    Public Property ExtracellularProteinViability As Integer = 5

    ''' <summary>胞外活性蛋白质每周期消耗的环境ATP量</summary>
    Public Property ExtracellularProteinATPCost As Integer = 1

    ''' <summary>胞外活性蛋白质执行功能的概率（0.0-1.0）</summary>
    Public Property ExtracellularProteinActivityProb As Double = 0.3

    ''' <summary>失活蛋白质被降解酶降解的概率（0.0-1.0）</summary>
    Public Property InactiveProteinDegradationProb As Double = 0.2

    ' ===== v4.0 外源DNA整合配置 =====

    ''' <summary>细胞内吞外源DNA的概率（0.0-1.0）</summary>
    Public Property DNAIngestionBaseProb As Double = 0.1

    ''' <summary>内吞外源DNA消耗的ATP量</summary>
    Public Property DNAIngestionATPCost As Integer = 5

    ''' <summary>外源DNA整合到基因组的概率（0.0-1.0，受基因组大小修正）</summary>
    Public Property DNAIntegrationBaseProb As Double = 0.5

    ''' <summary>外源DNA整合每个基因消耗的ATP量</summary>
    Public Property DNAIntegrationATPCostPerGene As Integer = 2

    ''' <summary>基因组最大基因数限制（防止无限整合）</summary>
    Public Property MaxGenomeGeneCount As Integer = 50

    ''' <summary>无整合/降解功能时外源DNA自发降解概率</summary>
    Public Property SpontaneousDNADegradationProb As Double = 0.05

    Public Overrides Function ToString() As String
        Return Me.GetJson
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Shared Function [Default]() As Configs
        Return New Configs
    End Function

End Class
