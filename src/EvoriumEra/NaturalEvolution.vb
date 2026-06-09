Imports EvoriumEra.BiologicalRules
Imports EvoriumEra.Data
Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Serialization.JSON
Imports RNG = Microsoft.VisualBasic.Math.RandomExtensions

''' <summary>
''' 用于模拟自然环境下的微生物群落演化过程的计算程序 v3.0
''' 
''' v2.0核心改进：
''' 1. 完整代谢链驱动交叉喂养
''' 2. 基因→蛋白质→功能的正确表达链
''' 3. 基因组维护成本驱动代谢专化
''' 4. 环境梯度创造生态位
''' 5. 营养补充维持群落持续演化
''' 6. 细胞裂解实现营养循环
''' 
''' v3.0新增：
''' 1. 温度系统：昼夜循环、深度梯度、代谢产热、蛋白热失活/冷休克
''' 2. 渗透压系统：离子强度、渗透压调节、相容溶质
''' 3. 扩展代谢网络：更多碳代谢路径、硫/铁/磷循环、扩展氨基酸和次级代谢
''' </summary>
Public Class NaturalEvolution

    ' ===== 核心成员 =====
    Public Property Env As NaturalEnvironment
    Public Property Scheduler As RuleScheduler
    Public Property Config As Configs

    ' ===== 状态 =====
    Public Property CurrentIteration As i32 = 0
    Public Property IsRunning As Boolean = False
    Public Property LivingCellCount As Integer = 0
    Public Property DeadCellCount As Integer = 0

    ' ===== v2.0 统计 =====
    Public Property CrossFeedingEvents As Long = 0

    ' ===== v3.0 统计 =====
    Public Property AverageTemperature As Double = 0
    Public Property AverageIonStrength As Double = 0
    ''' <summary>
    ''' 蛋白质变性事件计数
    ''' </summary>
    ''' <returns></returns>
    Public Property DenaturationEvents As Long = 0

    ''' <summary>
    ''' ===== 快照系统 =====
    ''' </summary>
    ''' <returns></returns>
    Public Property SnapshotManager As SnapshotManager

    ReadOnly debug As Boolean = False

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
    Public Sub New(config As Configs, snapshotRoot As String, Optional debug As Boolean = False)
        Me.Config = config
        Me.SnapshotManager = New SnapshotManager(snapshotRoot)
        Me.debug = debug
    End Sub

    Public Function Initialize() As NaturalEvolution
        Call VBDebugger.EchoLine("setup the natural evolution simulation system...")

        If debug Then
            Call "inspect of the experiment configs:".debug
            Call Config.GetJson.debug
        End If

        Env = New NaturalEnvironment(Config, debug:=debug)
        Scheduler = New RuleScheduler()
        CurrentIteration = 0

        ' 初始化环境分子
        InitializeEnvironment()

        ' 初始化细胞
        InitializeCells()

        ' [v3.0] 初始化温度
        InitializeTemperature()

        ' [v3.0] 初始化离子
        InitializeIons()

        Return Me
    End Function

    Public Sub Run(Optional maxSteps As Long = 9999)
        IsRunning = True

        While CInt(CurrentIteration) < maxSteps AndAlso IsRunning AndAlso App.Running
            Call VBDebugger.EchoLine($"[{CInt(CurrentIteration)}] living_cells: {LivingCellCount}; temperature[environment_avg]: {AverageTemperature:F2}℃")
            Call RunIteration(++CurrentIteration)
        End While
    End Sub

    Public Sub RunIteration(currentIteration As Integer)
        ' 1. 获取所有活细胞并随机打乱
        Dim cells = (From c As Cell
                     In Env.AllCells
                     Where c.IsAlive
                     Order By RNG.NextDouble).ToArray()
        Dim maxActions = Config.MaxCellActions

        ' 2. 环境级别规则（营养补充、扩散、温度）
        Call Scheduler.ExecuteEnvironmentRules(Env, currentIteration)

        ' 3. 对每个细胞执行规则
        If debug Then
            For Each cell As Cell In cells
                If cell.IsAlive Then
                    Call CellIteration(cell, maxActions)
                End If
            Next
        Else
            Call Parallel.ForEach(cells,
                 body:=Sub(cell)
                           If cell.IsAlive Then
                               Call CellIteration(cell, maxActions)
                           End If
                       End Sub)
        End If

        ' 4. HGT检查
        CheckHGT()

        ' 5. 更新统计
        UpdateStatistics()

        If currentIteration Mod SnapshotInterval = 0 Then
            SnapshotManager.SaveSnapshot(Me)
        End If
    End Sub

    Private Sub CellIteration(cell As Cell, maxActions As Integer)
        Dim actions As Integer = 0

        ' [v3.0] 更新细胞温度为所在格子温度
        Call SyncCellTemperature(cell)
        ' 全局规则（基因组维护、细胞裂解、温度、渗透压）
        Call Scheduler.ExecuteGlobalRules(cell, Env)

        ' 基于蛋白质丰度选择功能执行
        While actions < maxActions AndAlso cell.IsAlive AndAlso cell.ATP > 0
            Dim func = SelectFunctionByProteinAbundance(cell)

            If func Is Nothing Then
                Exit While
            End If

            Scheduler.ExecuteFunction(func, cell, Env)
            actions += 1
        End While

        ' 代谢溢流检查
        CheckOverflowSecretion(cell)

        ' 被动扩散（水、氧气等小分子）
        PassiveDiffusion(cell)

        ' [v3.0] 温度恢复（细胞温度向环境温度回归）
        cell.InternalTemperature = cell.InternalTemperature * 0.9 + GetVoxelTemperature(cell) * 0.1

        ' 死亡检查
        If cell.ATP <= 0 Then
            cell.ConsecutiveNoATP += 1
            If cell.ConsecutiveNoATP >= Config.StarvationDeathIterations Then
                Call Env.LyseCell(cell, reason:="structural_failure_due_to_ATP_depletion")
            End If
        Else
            cell.ConsecutiveNoATP = 0
        End If

        ' 年龄增长
        cell.Age += 1
    End Sub

    ''' <summary>
    ''' [v3.0] 同步细胞温度为所在格子温度
    ''' </summary>
    Private Sub SyncCellTemperature(cell As Cell)
        Dim voxel = Env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
        ' 细胞温度 = 0.7*环境温度 + 0.3*细胞内部温度
        cell.InternalTemperature = voxel.Temperature * 0.7 + cell.InternalTemperature * 0.3
    End Sub

    ''' <summary>
    ''' [v3.0] 获取细胞所在格子的温度
    ''' </summary>
    Private Function GetVoxelTemperature(cell As Cell) As Double
        Dim voxel = Env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
        Return voxel.Temperature
    End Function

    Private Function SelectFunctionByProteinAbundance(cell As Cell) As GeneOntology?
        If cell.Proteins.Count = 0 Then Return Nothing

        ' 构建蛋白质丰度加权的功能列表
        Dim candidates = New List(Of (func As GeneOntology, weight As Double))

        For Each kvp In cell.Proteins
            If kvp.Value <= 0 Then Continue For

            Dim w = CDbl(kvp.Value)

            ' ATP低时，能量代谢优先
            If cell.ATP < 500 Then
                If kvp.Key = GeneOntology.AerobicEnergyMetabolismATP OrElse
                   kvp.Key = GeneOntology.AnaerobicEnergyMetabolismATP OrElse
                   kvp.Key = GeneOntology.GlucoseConversionEnzyme OrElse
                   kvp.Key = GeneOntology.PyruvateEnzyme OrElse
                   kvp.Key = GeneOntology.AcetateEnzyme OrElse
                   kvp.Key = GeneOntology.LactateDehydrogenase OrElse
                   kvp.Key = GeneOntology.SuccinateEnzyme OrElse
                   kvp.Key = GeneOntology.EthanolMetabolism OrElse
                   kvp.Key = GeneOntology.FormateMetabolism OrElse
                   kvp.Key = GeneOntology.ButyrateEnzyme Then
                    w *= 3.0
                End If
            End If

            ' 氧气低时，厌氧代谢优先
            If cell.GetMoleculeAmount(MoleculeType.Oxygen) < 10 Then
                If kvp.Key = GeneOntology.AnaerobicEnergyMetabolismATP OrElse
                   kvp.Key = GeneOntology.LactateDehydrogenase Then
                    w *= 5.0
                End If
            End If

            ' 有抗生素时，降解抗生素优先
            If cell.GetMoleculeAmount(MoleculeType.Antibiotic) > 0 OrElse
               cell.GetMoleculeAmount(MoleculeType.Toxin) > 0 Then
                If kvp.Key = GeneOntology.DegradeAntibiotic Then
                    w *= 10.0
                End If
            End If

            ' [v3.0] 高温时，耐热蛋白优先表达
            If cell.InternalTemperature > 40 Then
                If kvp.Key = GeneOntology.Thermotolerance Then
                    w *= 8.0
                End If
            End If

            ' [v3.0] 低温时，冷休克响应优先
            If cell.InternalTemperature < 10 Then
                If kvp.Key = GeneOntology.ColdShockResponse Then
                    w *= 8.0
                End If
            End If

            ' [v3.0] 渗透压失衡时，渗透调节优先
            Dim voxel = Env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
            Dim osmDiff = Math.Abs(voxel.ExternalIonStrength - cell.InternalIonStrength)
            If osmDiff > 50 Then
                If kvp.Key = GeneOntology.Osmoregulation OrElse
                   kvp.Key = GeneOntology.CompatibleSoluteSynthesis Then
                    w *= 5.0
                End If
            End If

            candidates.Add((kvp.Key, w))
        Next

        If candidates.Count = 0 Then Return Nothing

        ' 加权随机选择
        Dim totalWeight = candidates.Sum(Function(c) c.weight)
        Dim r = RNG.NextDouble() * totalWeight
        Dim cumulative = 0.0

        For Each c As (func As GeneOntology, weight As Double) In candidates
            cumulative += c.weight
            If r <= cumulative Then Return c.func
        Next

        Return candidates.Last.func
    End Function

    ''' <summary>
    ''' [v2.0] 代谢溢流：中间产物超出容量60%时自动分泌50%
    ''' </summary>
    Private Sub CheckOverflowSecretion(cell As Cell)
        Dim capacity = Config.MaxCellContentCapacity
        Dim overflowMolecules = {
            MoleculeType.Pyruvate, MoleculeType.Acetate, MoleculeType.Lactate,
            MoleculeType.Succinate, MoleculeType.Ethanol, MoleculeType.Formate,
            MoleculeType.Butyrate, MoleculeType.FattyAcid, MoleculeType.Methane,
            MoleculeType.AminoMixGluFamily, MoleculeType.AminoMixAspFamily,
            MoleculeType.AminoMixSerGly, MoleculeType.AminoMixAromatic,
            MoleculeType.AminoMixBranched, MoleculeType.AminoMixThiol,
            MoleculeType.Vitamin, MoleculeType.Pigment, MoleculeType.Toxin
        }

        For Each mol In overflowMolecules
            Dim amount = cell.GetMoleculeAmount(mol)
            If amount > capacity * Config.OverflowThreshold Then
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
        Dim passiveMolecules = {
            MoleculeType.Water, MoleculeType.Oxygen, MoleculeType.CarbonDioxide,
            MoleculeType.HydrogenIon, MoleculeType.HydroxideIon,
            MoleculeType.CarbonSource, MoleculeType.NitrogenSource
        }

        For Each mol In passiveMolecules
            Dim internal = cell.GetMoleculeAmount(mol)
            Dim external = voxel.GetMoleculeAmount(mol)
            Dim diff = external - internal

            If Math.Abs(diff) > 5 Then
                Dim transfer = CInt(Math.Sign(diff) * Math.Min(Math.Abs(diff) * 0.05, 5))
                If transfer > 0 Then
                    cell.AddMoleculeInternal(mol, transfer)
                    If Not voxel.ExternalMolecules.ContainsKey(mol) Then
                        voxel.ExternalMolecules(mol) = Molecule.EmptyModel(mol)
                    End If
                    voxel.ExternalMolecules(mol).AddQuantity(-transfer)
                    If voxel.ExternalMolecules(mol) < 0 Then
                        voxel.ExternalMolecules(mol).SetQuantity(0)
                    End If
                End If
            End If
        Next
    End Sub

    Private Sub CheckHGT()
        Dim cells = Env.AllCells().Where(Function(c) c.IsAlive).ToList()
        For Each cell In cells
            Dim voxel = Env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
            Dim neighbors = Env.GetNeighbors(voxel)

            For Each neighbor In neighbors
                If neighbor.Occupant IsNot Nothing AndAlso neighbor.Occupant.IsAlive Then
                    If RNG.NextDouble() < 0.01 Then
                        ExchangePlasmids(cell, neighbor.Occupant)
                    End If
                End If
            Next
        Next
    End Sub

    Private Sub ExchangePlasmids(cell1 As Cell, cell2 As Cell)
        If cell1.Plasmids.Any() AndAlso cell2.Plasmids.Any() Then
            Dim p1 = cell1.Plasmids(RNG.Next(cell1.Plasmids.Count))
            Dim p2 = cell2.Plasmids(RNG.Next(cell2.Plasmids.Count))
            cell1.Plasmids.Remove(p1)
            cell2.Plasmids.Remove(p2)
            cell1.Plasmids.Add(p2)
            cell2.Plasmids.Add(p1)
        ElseIf cell1.Plasmids.Any() Then
            Dim p = cell1.Plasmids(RNG.Next(cell1.Plasmids.Count))
            cell1.Plasmids.Remove(p)
            cell2.Plasmids.Add(p)
        ElseIf cell2.Plasmids.Any() Then
            Dim p = cell2.Plasmids(RNG.Next(cell2.Plasmids.Count))
            cell2.Plasmids.Remove(p)
            cell1.Plasmids.Add(p)
        End If
    End Sub

    Private Sub InitializeEnvironment()
        Dim dims = Env.Dimensions

        ' 基础环境分子
        For x As Integer = 0 To dims.Width - 1
            For y As Integer = 0 To dims.Height - 1
                For z As Integer = 0 To dims.Depth - 1
                    Dim voxel = Env.Grid(x, y, z)

                    Call Env.AddMolecule(voxel, MoleculeType.Water, RNG.NextInteger(50, 200))
                    Call Env.AddMolecule(voxel, MoleculeType.CarbonSource, RNG.NextInteger(20, 80))
                    Call Env.AddMolecule(voxel, MoleculeType.NitrogenSource, RNG.NextInteger(10, 40))
                    Call Env.AddMolecule(voxel, MoleculeType.Glucose, RNG.NextInteger(5, 30))

                    ' 氧气梯度：表层多，深层少
                    Dim targetOxygen = Math.Max(0, Config.SurfaceOxygenLevel - z * Config.OxygenDecayPerLayer)

                    Call Env.AddMolecule(voxel, MoleculeType.Oxygen, CInt(targetOxygen))
                    Call Env.AddMolecule(voxel, MoleculeType.CarbonDioxide, RNG.NextInteger(5, 20))
                Next
            Next
        Next
    End Sub

    ''' <summary>
    ''' [v3.0] 初始化温度梯度
    ''' </summary>
    Private Sub InitializeTemperature()
        Dim dims = Env.Dimensions

        For x As Integer = 0 To dims.Width - 1
            For y As Integer = 0 To dims.Height - 1
                For z As Integer = 0 To dims.Depth - 1
                    Dim voxel = Env.Grid(x, y, z)
                    ' 表层温度高，深层温度低
                    voxel.Temperature = Config.BaseTemperature - z * Config.TemperatureDepthDecay
                Next
            Next
        Next
    End Sub

    ''' <summary>
    ''' [v3.0] 初始化环境离子
    ''' </summary>
    Private Sub InitializeIons()
        Dim dims = Env.Dimensions

        For x As Integer = 0 To dims.Width - 1
            For y As Integer = 0 To dims.Height - 1
                For z As Integer = 0 To dims.Depth - 1
                    Dim voxel = Env.Grid(x, y, z)

                    Call Env.AddMolecule(voxel, MoleculeType.SodiumIon, Config.InitialSaltIonLevel + RNG.NextInteger(-10, 10))
                    Call Env.AddMolecule(voxel, MoleculeType.PotassiumIon, Config.InitialSaltIonLevel \ 2 + RNG.NextInteger(-5, 5))
                    Call Env.AddMolecule(voxel, MoleculeType.ChlorideIon, Config.InitialSaltIonLevel + RNG.NextInteger(-10, 10))
                    Call Env.AddMolecule(voxel, MoleculeType.Phosphate, Config.InitialPhosphateLevel + RNG.NextInteger(-5, 5))
                    Call Env.AddMolecule(voxel, MoleculeType.Sulfate, Config.InitialSulfateLevel + RNG.NextInteger(-5, 5))
                    Call Env.AddMolecule(voxel, MoleculeType.IronII, Config.InitialIronLevel \ 2 + RNG.NextInteger(-3, 3))
                    Call Env.AddMolecule(voxel, MoleculeType.IronIII, Config.InitialIronLevel \ 3 + RNG.NextInteger(-2, 2))
                    Call Env.AddMolecule(voxel, MoleculeType.MagnesiumIon, RNG.NextInteger(5, 15))
                    Call Env.AddMolecule(voxel, MoleculeType.CalciumIon, RNG.NextInteger(3, 10))
                Next
            Next
        Next
    End Sub

    Private Function RequestVoxel() As Voxel
        Dim dims = Env.Dimensions

        Do
            Dim x = RNG.Next(dims.Width)
            Dim y = RNG.Next(dims.Height)
            Dim z = RNG.Next(dims.Depth)

            Dim voxel = Env.Grid(x, y, z)

            If voxel.Occupant Is Nothing Then
                Return voxel
            End If
        Loop
    End Function

    Private Sub InitializeCells()
        Dim allFunctions = [Enum].GetValues(GetType(GeneOntology)).Cast(Of GeneOntology)().ToList()

        For i As Integer = 1 To Config.InitCellNumbers
            Dim voxel As Voxel = RequestVoxel()
            Dim geneCount = RNG.NextInteger(Config.InitGenomeMinGenes, Config.InitGenomeMaxGenes + 1)
            Dim genome = New Replicon With {
                .IsPlasmid = False,
                .Genes = New List(Of Gene)
            }

            ' 确保每个细胞至少有基础代谢基因
            Dim essentialGenes = {
                GeneOntology.GeneTranscription,
                GeneOntology.ProteinTranslation,
                GeneOntology.ReplicateDNA,
                GeneOntology.CellDivision,
                GeneOntology.Endocytosis
            }

            For Each essential In essentialGenes
                genome.Genes.Add(New Gene With {.FunctionOntology = essential})
            Next

            ' 添加随机基因
            For j = 1 To geneCount - essentialGenes.Length
                genome.Genes.Add(New Gene With {.FunctionOntology = allFunctions(RNG.Next(allFunctions.Count))})
            Next

            Dim cell = New Cell With {
                .Genome = genome,
                .Position = New SpatialIndex3D(voxel.Position),
                .ATP = 200,
                .Age = 0,
                .DivisionCount = 0
            }

            voxel.Occupant = cell

            ' 初始化蛋白质（基于基因组，初始有少量蛋白质）
            For Each gene In cell.Genome.Genes
                If Not cell.Proteins.ContainsKey(gene.FunctionOntology) Then
                    cell.Proteins(gene.FunctionOntology) = 0
                End If
                cell.Proteins(gene.FunctionOntology) += RNG.NextInteger(1, 3)
            Next

            ' 初始分子
            cell.AddMoleculeInternal(MoleculeType.Water, RNG.NextInteger(50, 150))
            cell.AddMoleculeInternal(MoleculeType.Glucose, RNG.NextInteger(10, 30))
            cell.AddMoleculeInternal(MoleculeType.CarbonSource, RNG.NextInteger(10, 30))
            cell.AddMoleculeInternal(MoleculeType.NitrogenSource, RNG.NextInteger(5, 20))
            cell.AddMoleculeInternal(MoleculeType.Nucleotide, RNG.NextInteger(10, 30))
            cell.AddMoleculeInternal(MoleculeType.AminoMixGluFamily, RNG.NextInteger(5, 20))
            cell.AddMoleculeInternal(MoleculeType.AminoMixAspFamily, RNG.NextInteger(5, 20))
            cell.AddMoleculeInternal(MoleculeType.AminoMixSerGly, RNG.NextInteger(5, 20))

            ' [v3.0] 初始离子
            cell.AddMoleculeInternal(MoleculeType.SodiumIon, RNG.NextInteger(5, 15))
            cell.AddMoleculeInternal(MoleculeType.PotassiumIon, RNG.NextInteger(10, 30))
            cell.AddMoleculeInternal(MoleculeType.ChlorideIon, RNG.NextInteger(5, 15))
            cell.AddMoleculeInternal(MoleculeType.Phosphate, RNG.NextInteger(5, 15))
            cell.AddMoleculeInternal(MoleculeType.MagnesiumIon, RNG.NextInteger(2, 5))

            ' [v3.0] 初始温度
            cell.InternalTemperature = voxel.Temperature
        Next
    End Sub

    Private Sub UpdateStatistics()
        Dim cells = Env.AllCells().ToArray

        LivingCellCount = cells.Count(Function(c) c.IsAlive)
        DeadCellCount = cells.Count(Function(c) Not c.IsAlive)

        ' [v3.0] 更新温度和离子统计
        Dim tempSum = 0.0
        Dim ionSum = 0.0
        Dim count = 0
        Dim dims = Env.Dimensions

        For x As Integer = 0 To dims.Width - 1 Step 5
            For Y As Integer = 0 To dims.Height - 1 Step 5
                For z As Integer = 0 To dims.Depth - 1 Step 5
                    Dim voxel = Env.Grid(x, Y, z)
                    tempSum += voxel.Temperature
                    ionSum += voxel.ExternalIonStrength
                    count += 1
                Next
            Next
        Next

        If count > 0 Then
            AverageTemperature = tempSum / count
            AverageIonStrength = ionSum / count
        End If
    End Sub
End Class
