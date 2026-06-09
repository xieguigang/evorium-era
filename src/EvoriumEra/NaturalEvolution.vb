Imports EvoriumEra.BiologicalRules
Imports EvoriumEra.BiologicalRules.Rules
Imports EvoriumEra.Data
Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports Microsoft.VisualBasic.Imaging
Imports RNG = Microsoft.VisualBasic.Math.RandomExtensions

''' <summary>
''' 用于模拟自然环境下的微生物群落演化过程的计算程序 v2.0
''' 
''' v2.0核心改进：
''' 1. 完整代谢链驱动交叉喂养
''' 2. 基因→蛋白质→功能的正确表达链
''' 3. 基因组维护成本驱动代谢专化
''' 4. 环境梯度创造生态位
''' 5. 营养补充维持群落持续演化
''' 6. 细胞裂解实现营养循环
''' </summary>
Public Class NaturalEvolution

    Public Property CurrentEnvironment As NaturalEnvironment

    ' ===== 核心成员 =====
    Public Property Env As NaturalEnvironment
    Public Property Scheduler As RuleScheduler
    Public Property Config As Configs

    ' ===== 状态 =====
    Public Property CurrentIteration As Long = 0
    Public Property IsRunning As Boolean = False
    Public Property LivingCellCount As Integer = 0
    Public Property DeadCellCount As Integer = 0

    ' ===== v2.0 统计 =====
    Public Property CrossFeedingEvents As Long = 0
    Public Property TotalDivisions As Long = 0
    Public Property TotalMutations As Long = 0
    Public Property TotalLysis As Long = 0

    ''' <summary>
    ''' ===== 快照系统 =====
    ''' </summary>
    ''' <returns></returns>
    Public Property SnapshotManager As SnapshotManager

    ''' <summary>
    ''' 每 N 步存一次
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property SnapshotInterval As Integer
        Get
            Return Config.SnapshotInterval
        End Get
    End Property

    ''' <summary>
    ''' ===== 初始化 =====
    ''' </summary>
    ''' <param name="config"></param>
    ''' <param name="snapshotRoot">
    ''' A temp dir path for save the snapshot temp data and result zip package file
    ''' </param>
    Public Sub New(config As Configs, snapshotRoot As String)
        Me.Config = config
        Me.SnapshotManager = New SnapshotManager(snapshotRoot)
    End Sub

    Public Function Initialize() As NaturalEvolution
        Env = New NaturalEnvironment(Config)
        Scheduler = New RuleScheduler()

        ' 初始化环境
        InitializeEnvironment()

        ' 初始化细胞
        InitializeCells(Config.InitCellNumbers)

        ' 初始化营养热点
        InitializeNutrientHotspots()

        CurrentIteration = 0

        Return Me
    End Function

    Public Sub Run(Optional maxSteps As Long = 9999)
        IsRunning = True

        While CurrentIteration < maxSteps AndAlso IsRunning AndAlso App.Running
            Call RunIteration()
        End While
    End Sub

    Public Sub RunIteration()
        CurrentIteration += 1

        ' 1. 环境级别规则（营养补充、扩散）
        Scheduler.ExecuteEnvironmentRules(Env, Config)

        ' 2. 获取所有活细胞并随机打乱顺序
        Dim cells = Env.AllCells().Where(Function(c) c.IsAlive).ToList()
        Shuffle(cells)

        ' 3. 对每个细胞执行规则
        For Each cell In cells
            If Not cell.IsAlive Then Continue For

            ' 3a. 全局规则（基因组维护成本）
            Scheduler.ExecuteGlobalRules(cell, Env)

            ' 3b. 选择并执行基因功能
            Dim actions = SelectActions(cell)
            For Each action In actions
                If cell.IsAlive Then
                    Scheduler.ExecuteFunction(action, cell, Env)
                End If
            Next

            ' 3c. 代谢溢流检查
            CheckOverflowSecretion(cell)

            ' 3d. 被动扩散（小分子自动进出细胞）
            PassiveDiffusion(cell)

            ' 3e. 死亡检查
            CheckDeath(cell)

            ' 3f. 更新年龄
            cell.Age += 1
        Next

        ' 4. 处理死亡细胞（裂解释放内容物）
        ProcessDeadCells()

        ' 5. 更新统计
        UpdateStatistics()

        If CurrentIteration Mod SnapshotInterval = 0 Then
            SnapshotManager.SaveSnapshot(Me)
        End If
    End Sub

    ''' <summary>
    ''' [v2.0] 基于优先级和蛋白质丰度选择要执行的功能
    ''' </summary>
    Private Function SelectActions(cell As Cell) As List(Of GeneOntology)
        Dim actions = New List(Of GeneOntology)()
        Dim maxActions = Config.MaxCellActions

        ' 收集所有可执行的功能（基于已有蛋白质）
        Dim availableFunctions = New List(Of (func As GeneOntology, priority As Double))()

        For Each kvp In cell.Proteins
            If kvp.Value > 0 Then
                Dim priority = CalculatePriority(cell, kvp.Key)
                availableFunctions.Add((kvp.Key, priority))
            End If
        Next

        ' 按优先级排序
        availableFunctions = availableFunctions.OrderByDescending(Function(x) x.priority).ToList()

        ' 选择前N个功能
        For Each item In availableFunctions
            If actions.Count >= maxActions Then Exit For

            ' 按概率选择（高优先级的功能更可能被选中）
            If RNG.NextDouble() < Math.Min(1.0, item.priority) Then
                actions.Add(item.func)
            End If
        Next

        ' 确保至少执行能量代谢（如果有蛋白质的话）
        If actions.Count = 0 AndAlso availableFunctions.Any() Then
            actions.Add(availableFunctions.First.func)
        End If

        Return actions
    End Function

    ''' <summary>
    ''' [v2.0] 计算功能的执行优先级
    ''' </summary>
    Private Function CalculatePriority(cell As Cell, func As GeneOntology) As Double
        Dim priority = 1.0

        ' ATP低时，能量代谢优先
        If cell.ATP < 500 Then
            If func = GeneOntology.AerobicEnergyMetabolismATP OrElse
               func = GeneOntology.AnaerobicEnergyMetabolismATP Then
                priority *= 5.0
            End If
            If func = GeneOntology.GlucoseConversionEnzyme Then
                priority *= 3.0
            End If
            If func = GeneOntology.PyruvateEnzyme Then
                priority *= 2.5
            End If
            If func = GeneOntology.AcetateEnzyme Then
                priority *= 2.0
            End If
        End If

        ' 有抗生素时，降解抗生素优先
        If cell.GetMoleculeAmount(MoleculeType.Antibiotic) > 0 Then
            If func = GeneOntology.DegradeAntibiotic Then
                priority *= 10.0
            End If
        End If

        ' 缺氧时，厌氧代谢优先
        If cell.GetMoleculeAmount(MoleculeType.Oxygen) < 10 Then
            If func = GeneOntology.AnaerobicEnergyMetabolismATP Then
                priority *= 5.0
            End If
            If func = GeneOntology.LactateDehydrogenase Then
                priority *= 3.0
            End If
        End If

        ' 蛋白质丰度加权
        Dim proteinCount = cell.Proteins.GetValueOrDefault(func)
        priority *= (1.0 + proteinCount * 0.2)

        Return priority
    End Function

    ''' <summary>
    ''' [v2.0] 代谢溢流：中间产物超出容量60%时自动分泌50%
    ''' </summary>
    Private Sub CheckOverflowSecretion(cell As Cell)
        Dim capacity = Config.MaxCellContentCapacity
        Dim threshold = capacity * 0.6

        ' 检查代谢中间产物
        Dim intermediates = {
            MoleculeType.Pyruvate, MoleculeType.Acetate, MoleculeType.Lactate,
            MoleculeType.AminoMixGluFamily, MoleculeType.AminoMixAspFamily,
            MoleculeType.AminoMixSerGly, MoleculeType.Nucleotide
        }

        For Each mol In intermediates
            Dim amount = cell.GetMoleculeAmount(mol)
            If amount > threshold * 0.1 Then ' 单个分子超过容量的6%
                Dim secretion = CInt(amount * Config.OverflowSecretionFraction)
                If secretion > 0 Then
                    cell.AddMoleculeInternal(mol, -secretion)
                    Env.AddMolecule(cell, mol, secretion)
                    CrossFeedingEvents += 1
                End If
            End If
        Next
    End Sub

    Private Sub PassiveDiffusion(cell As Cell)
        Dim voxel = Env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)

        ' 小分子被动扩散（进出细胞）
        Dim passiveMols = {
            MoleculeType.Water, MoleculeType.Oxygen, MoleculeType.CarbonDioxide,
            MoleculeType.HydrogenIon, MoleculeType.HydroxideIon
        }

        For Each mol In passiveMols
            Dim internal = cell.GetMoleculeAmount(mol)
            Dim external = voxel.GetMoleculeAmount(mol)
            Dim diff = external - internal

            If Math.Abs(diff) > 2 Then
                Dim transfer = CInt(Math.Sign(diff) * Math.Min(Math.Abs(diff) * 0.1, 10))
                If transfer > 0 Then
                    ' 从环境进入细胞
                    cell.AddMoleculeInternal(mol, transfer)
                    If voxel.ExternalMolecules.ContainsKey(mol) Then
                        voxel.ExternalMolecules(mol) -= transfer
                    End If
                ElseIf transfer < 0 Then
                    ' 从细胞进入环境
                    cell.AddMoleculeInternal(mol, transfer)
                    If Not voxel.ExternalMolecules.ContainsKey(mol) Then
                        voxel.ExternalMolecules(mol) = 0
                    End If
                    voxel.ExternalMolecules(mol) -= transfer
                End If
            End If
        Next
    End Sub

    Private Sub CheckDeath(cell As Cell)
        If cell.ATP <= 0 Then
            cell.ConsecutiveNoATP += 1
            If cell.ConsecutiveNoATP >= Config.StarvationDeathIterations Then
                cell.IsAlive = False
                TotalLysis += 1
            End If
        Else
            cell.ConsecutiveNoATP = 0
        End If
    End Sub

    Private Sub ProcessDeadCells()
        Dim lysisRule = New CellLysisRule()
        Dim cells = Env.AllCells().Where(Function(c) Not c.IsAlive).ToList()

        For Each cell In cells
            lysisRule.Execute(cell, Env)
        Next
    End Sub

    Private Sub InitializeEnvironment()
        Dim dims = Env.Dimensions

        ' 基础环境分子
        For x As Integer = 0 To dims.Width - 1
            For y As Integer = 0 To dims.Height - 1
                For z As Integer = 0 To dims.Depth - 1
                    Dim voxel = Env.Grid(x, y, z)

                    ' 氧气梯度：表层多，深层少
                    Dim oxygenLevel = Math.Max(10, Config.SurfaceOxygenLevel - z * Config.OxygenDecayPerLayer)
                    voxel.ExternalMolecules(MoleculeType.Oxygen) = oxygenLevel

                    ' 基础水分
                    voxel.ExternalMolecules(MoleculeType.Water) = 500

                    ' 基础碳源和氮源
                    voxel.ExternalMolecules(MoleculeType.CarbonSource) = RNG.NextInteger(20, 80)
                    voxel.ExternalMolecules(MoleculeType.NitrogenSource) = RNG.NextInteger(10, 40)

                    ' 少量glucose
                    voxel.ExternalMolecules(MoleculeType.Glucose) = RNG.NextInteger(5, 30)

                    ' CO2
                    voxel.ExternalMolecules(MoleculeType.CarbonDioxide) = RNG.NextInteger(5, 20)
                Next
            Next
        Next
    End Sub

    Private Sub InitializeCells(count As Integer)
        Dim allFunctions = [Enum].GetValues(GetType(GeneOntology)).Cast(Of GeneOntology)().ToList()

        ' 必需基因（每个细胞都必须有）
        Dim essentialGenes = {
            GeneOntology.GeneTranscription,
            GeneOntology.ProteinTranslation,
            GeneOntology.ReplicateDNA,
            GeneOntology.CellDivision
        }

        ' 代谢核心基因（大部分细胞应该有）
        Dim coreMetabolicGenes = {
            GeneOntology.AerobicEnergyMetabolismATP,
            GeneOntology.GlucoseConversionEnzyme,
            GeneOntology.Endocytosis,
            GeneOntology.Exocytosis
        }

        ' 可选基因（随机分配，创造初始多样性）
        Dim optionalGenes = {
            GeneOntology.AnaerobicEnergyMetabolismATP,
            GeneOntology.PyruvateEnzyme,
            GeneOntology.AcetateEnzyme,
            GeneOntology.LactateDehydrogenase,
            GeneOntology.AminoMixGluFamilyEnzyme,
            GeneOntology.AminoMixAspFamilyEnzyme,
            GeneOntology.AminoMixSerGlyEnzyme,
            GeneOntology.NucleicAcidSynthesis,
            GeneOntology.FlagellarMovement,
            GeneOntology.QuorumSensing,
            GeneOntology.SignalMoleculeSynthesis,
            GeneOntology.BiofilmSynthesis,
            GeneOntology.CellWallSynthesis,
            GeneOntology.SynthesizeAntibiotic,
            GeneOntology.DegradeAntibiotic,
            GeneOntology.DegradeMacromolecule,
            GeneOntology.CarbonFixation,
            GeneOntology.AcidMetabolism,
            GeneOntology.BaseMetabolism,
            GeneOntology.SecondaryMetaboliteSynthesis,
            GeneOntology.SiderophoreSynthesis,
            GeneOntology.DNAIntegration,
            GeneOntology.NucleicAcidDegradation,
            GeneOntology.ProteinDegradation
        }
        Dim dims = Env.Dimensions

        For i As Integer = 1 To count
            Dim cell = New Cell()

            ' 随机位置
            Dim x = RNG.NextInteger(5, dims.Width - 5)
            Dim y = RNG.NextInteger(5, dims.Height - 5)
            Dim z = RNG.NextInteger(0, CInt(dims.Depth * 0.7)) ' 主要在表层和中层
            cell.Position = New SpatialIndex3D(x, y, z)

            ' 构建基因组
            Dim genes = New List(Of Gene)()

            ' 添加必需基因
            For Each essential In essentialGenes
                genes.Add(New Gene With {.FunctionOntology = essential})
            Next

            ' 添加核心代谢基因（80%概率）
            For Each core In coreMetabolicGenes
                If RNG.NextDouble() < 0.8 Then
                    genes.Add(New Gene With {.FunctionOntology = core})
                End If
            Next

            ' 添加随机可选基因（每个30-60%概率）
            For Each [optional] As GeneOntology In optionalGenes
                If RNG.NextDouble() < RNG.NextDouble() * 0.4 + 0.2 Then
                    genes.Add(New Gene With {.FunctionOntology = [optional]})
                End If
            Next

            cell.Genome = New Replicon With {.Genes = genes, .IsPlasmid = False}

            ' 30%概率携带质粒
            If RNG.NextDouble() < 0.3 Then
                Dim plasmidGenes = New List(Of Gene)()
                Dim plasmidSize = RNG.NextInteger(1, 4)
                For j As Integer = 0 To plasmidSize - 1
                    plasmidGenes.Add(New Gene With {
                        .FunctionOntology = optionalGenes(RNG.Next(optionalGenes.Length))
                    })
                Next
                cell.Plasmids.Add(New Replicon With {.Genes = plasmidGenes, .IsPlasmid = True})
            End If

            ' 初始化蛋白质（基于基因组，初始有少量蛋白质）
            For Each gene In cell.Genome.Genes
                If Not cell.Proteins.ContainsKey(gene.FunctionOntology) Then
                    cell.Proteins(gene.FunctionOntology) = 0
                End If
                cell.Proteins(gene.FunctionOntology) += RNG.NextInteger(1, 3)
            Next

            ' 初始化分子
            cell.ATP = RNG.NextInteger(150, 300)
            cell.AddMoleculeInternal(MoleculeType.Water, RNG.NextInteger(200, 500))
            cell.AddMoleculeInternal(MoleculeType.Glucose, RNG.NextInteger(10, 50))
            cell.AddMoleculeInternal(MoleculeType.CarbonSource, RNG.NextInteger(20, 60))
            cell.AddMoleculeInternal(MoleculeType.NitrogenSource, RNG.NextInteger(10, 30))
            cell.AddMoleculeInternal(MoleculeType.Nucleotide, RNG.NextInteger(10, 30))
            cell.AddMoleculeInternal(MoleculeType.AminoMixGluFamily, RNG.NextInteger(5, 20))
            cell.AddMoleculeInternal(MoleculeType.AminoMixAspFamily, RNG.NextInteger(5, 20))
            cell.AddMoleculeInternal(MoleculeType.AminoMixSerGly, RNG.NextInteger(5, 20))

            ' 放置到环境中
            Dim voxel = Env.Grid(x, y, z)
            If voxel.Occupant Is Nothing Then
                voxel.Occupant = cell
            End If
        Next
    End Sub

    Private Sub InitializeNutrientHotspots()
        ' 营养热点在NutrientReplenishmentRule中处理
    End Sub

    Private Sub Shuffle(Of T)(list As List(Of T))
        Dim n = list.Count
        While n > 1
            n -= 1
            Dim k = RNG.Next(n + 1)
            Dim temp = list(k)
            list(k) = list(n)
            list(n) = temp
        End While
    End Sub

    Private Sub UpdateStatistics()
        Dim cells = Env.AllCells()
        LivingCellCount = cells.Count(Function(c) c.IsAlive)
        DeadCellCount = cells.Count(Function(c) Not c.IsAlive)
    End Sub
End Class
