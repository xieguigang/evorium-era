Imports EvoriumEra.BiologicalRules
Imports Microsoft.VisualBasic.Imaging
Imports RNG = Microsoft.VisualBasic.Math.RandomExtensions

Public Class Simulation

    Public Property CurrentEnvironment As Environment3D

    ' ===== 核心成员 =====
    Public Property Env As Environment3D
    Public Property Scheduler As RuleScheduler

    ' ===== 状态 =====
    Public Property CurrentIteration As Long = 0
    Public Property IsRunning As Boolean = False

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
            Return configs.SnapshotInterval
        End Get
    End Property

    ' ===== 统计 =====
    Public Property LivingCellCount As Integer = 0
    Public Property DeadCellCount As Integer = 0

    Friend ReadOnly configs As Configs
    Friend ReadOnly moleculeUtils As MoleculeUtils

    ' ===== 初始化 =====
    Public Sub New(config As Configs, snapshotRoot As String)
        Env = New Environment3D(config)
        configs = config
        CurrentEnvironment = Env
        Scheduler = New RuleScheduler()
        SnapshotManager = New SnapshotManager(snapshotRoot)
        moleculeUtils = New MoleculeUtils(configs, Env)

        InitializeWorld()
    End Sub

    Private Sub InitializeWorld()
        ' 初始化环境分子
        For Each v In Env.AllVoxels()
            InitVoxelMolecules(v)
        Next

        ' 初始化细胞
        For i As Integer = 1 To configs.InitCellNumbers
            SpawnRandomCell()
        Next

        UpdateStatistics()
    End Sub

    Private Sub InitVoxelMolecules(v As Voxel)
        v.ExternalMolecules(MoleculeType.Water) = RNG.NextInteger(500, 2000)
        v.ExternalMolecules(MoleculeType.Oxygen) = RNG.NextInteger(100, 800)
        v.ExternalMolecules(MoleculeType.CarbonSource) = RNG.NextInteger(200, 1000)
        v.ExternalMolecules(MoleculeType.NitrogenSource) = RNG.NextInteger(200, 800)
        v.ExternalMolecules(MoleculeType.Glucose) = RNG.NextInteger(50, 300)
    End Sub

    Private Sub SpawnRandomCell()
        Dim empty = Env.GetRandomEmptyVoxel()
        If empty Is Nothing Then Return

        Dim cell = New Cell()
        cell.Position = New SpatialIndex3D(empty.Position)

        ' 随机基因组
        cell.Genome = RandomGenome(RNG.NextInteger(5, 15))

        ' 初始代谢物
        cell.InternalMolecules(MoleculeType.ATP) = RNG.NextInteger(200, 600)
        cell.InternalMolecules(MoleculeType.Water) = RNG.NextInteger(200, 500)
        cell.InternalMolecules(MoleculeType.Nucleotide) = RNG.NextInteger(50, 300)

        cell.TotalMolecules = cell.InternalMolecules.Values.Sum()
        empty.Occupant = cell
    End Sub

    Private Function RandomGenome(geneCount As Integer) As Replicon
        Dim r As New Replicon()
        Dim allFuncs = [Enum].GetValues(GetType(GeneOntology)).Cast(Of GeneOntology)().ToList()

        For i = 1 To geneCount
            r.Genes.Add(New Gene() With {
                .FunctionOntology = allFuncs(RNG.Next(allFuncs.Count))
            })
        Next
        Return r
    End Function

    Public Sub Run(Optional maxSteps As Long = 9999)
        IsRunning = True

        While CurrentIteration < maxSteps AndAlso IsRunning AndAlso App.Running
            Call StepOnce()
        End While
    End Sub

    Public Sub StepOnce()
        CurrentIteration += 1

        ' 1. 打乱体素顺序（消除空间偏差）
        Dim voxels = Env.AllVoxels().OrderBy(Function(v) RNG.NextDouble()).ToList()

        ' 2. 每个格子最多执行 5 次操作
        For Each v As Voxel In voxels
            For op As Integer = 1 To configs.MaxVoxelActions
                ProcessVoxel(v)
            Next
        Next

        ' 3. 细胞级更新
        Dim allCells = Env.AllCells()
        For Each cell In allCells
            If cell.IsAlive Then
                CellUpdate(cell)
            End If
        Next

        ' 4. 全局扩散
        DiffuseAllVoxels()

        ' 5. 统计 & 快照
        UpdateStatistics()

        If CurrentIteration Mod SnapshotInterval = 0 Then
            SnapshotManager.SaveSnapshot(Me)
        End If
    End Sub

    Private Sub ProcessVoxel(v As Voxel)
        ' 非被动扩散分子在相邻格子间交换
        Dim neighbors = Env.GetNeighbors(v)
        If Not neighbors.Any() Then Return

        Dim target = neighbors(RNG.Next(neighbors.Count))

        For Each mol In v.ExternalMolecules.Keys.ToList()
            If Not IsPassiveDiffusion(mol) Then
                Dim amount = RNG.NextInteger(1, 6)
                TransferBetweenVoxels(v, target, mol, amount)
            End If
        Next
    End Sub

    Private Sub TransferBetweenVoxels(
        fromV As Voxel, toV As Voxel, mol As MoleculeType, amount As Integer)

        If Not fromV.ExternalMolecules.ContainsKey(mol) Then Return
        If fromV.ExternalMolecules(mol) < amount Then Return

        moleculeUtils.AddMolecule(fromV, mol, -amount)
        moleculeUtils.AddMolecule(toV, mol, amount)
    End Sub

    Private Sub CellUpdate(cell As Cell)
        ' ===== ATP 死亡判定（规则 3）=====
        If cell.ATP <= 0 Then
            cell.ConsecutiveNoATP += 1
            If cell.ConsecutiveNoATP >= 5 Then
                KillCell(cell)
                Return
            End If
        Else
            cell.ConsecutiveNoATP = 0
        End If

        ' ===== pH 致死（规则 18 / 19）=====
        If cell.InternalMolecules.ContainsKey(MoleculeType.HydroxideIon) AndAlso
           cell.InternalMolecules(MoleculeType.HydroxideIon) >= 20 Then
            KillCell(cell)
            Return
        End If

        ' ===== 容量破裂（规则 17）=====
        If cell.TotalMolecules > configs.MaxCellContentCapacity Then
            KillCell(cell)
            Return
        End If

        ' ===== 轮盘赌选择功能（规则 26）=====
        For i As Integer = 1 To configs.MaxCellActions
            Dim func = RouletteSelect(cell)
            If func.HasValue Then
                Scheduler.ExecuteFunction(func.Value, cell, Env)
            End If
        Next
    End Sub

    Private Function RouletteSelect(cell As Cell) As GeneOntology?
        If cell.Proteins.Count = 0 Then Return Nothing

        Dim totalWeight = cell.Proteins.Values.Sum()
        If totalWeight <= 0 Then Return Nothing

        Dim r = RNG.NextDouble() * totalWeight

        For Each kv In cell.Proteins
            r -= kv.Value
            If r <= 0 Then Return kv.Key
        Next

        Return cell.Proteins.Keys.Last()
    End Function

    Private Sub KillCell(cell As Cell)
        cell.IsAlive = False
        moleculeUtils.LyseCell(cell)
        DeadCellCount += 1
    End Sub

    ''' <summary>
    ''' 全局扩散
    ''' </summary>
    Private Sub DiffuseAllVoxels()
        Dim voxels = Env.AllVoxels().ToList()

        For Each v In voxels
            Dim neighbors = Env.GetNeighbors(v)
            If Not neighbors.Any() Then Continue For

            Dim target = neighbors(RNG.Next(neighbors.Count))

            For Each mol In v.ExternalMolecules.Keys.ToList()
                If IsPassiveDiffusion(mol) Then
                    Dim delta = v.ExternalMolecules(mol) -
                                target.ExternalMolecules(mol)

                    Dim transfer = CInt(delta * 0.05)
                    If transfer > 0 Then
                        moleculeUtils.AddMolecule(v, mol, -transfer)
                        moleculeUtils.AddMolecule(target, mol, transfer)
                    End If
                End If
            Next
        Next
    End Sub

    Private Function IsPassiveDiffusion(mol As MoleculeType) As Boolean
        Return mol = MoleculeType.Water OrElse
               mol = MoleculeType.Oxygen OrElse
               mol = MoleculeType.CarbonDioxide OrElse
               mol = MoleculeType.HydrogenIon OrElse
               mol = MoleculeType.HydroxideIon OrElse
               mol = MoleculeType.CarbonSource OrElse
               mol = MoleculeType.NitrogenSource
    End Function

    Private Sub UpdateStatistics()
        Dim cells = Env.AllCells()
        LivingCellCount = cells.Count(Function(c) c.IsAlive)
        DeadCellCount = cells.Count(Function(c) Not c.IsAlive)
    End Sub
End Class