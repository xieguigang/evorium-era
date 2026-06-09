Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Linq

Namespace Models.Container

    Public Class Cell : Implements IVoxel

        Public Property ID As Guid = Guid.NewGuid()
        Public Property ParentID As Guid? = Nothing
        Public Property Generation As Integer = 0
        Public Property Position As SpatialIndex3D Implements IVoxel.Position
        Public Property Genome As Replicon
        Public Property Plasmids As New List(Of Replicon)
        Public Property InternalMolecules As New Dictionary(Of MoleculeType, Molecule) Implements IVoxel.Molecules
        Public Property Proteins As New Dictionary(Of GeneOntology, Integer)
        Public Property HasCellWall As Boolean = False
        Public Property IsAlive As Boolean = True
        Public Property ATP As Integer = 200
        ''' <summary>
        ''' 连续缺乏三磷酸腺苷的迭代次数
        ''' </summary>
        ''' <returns></returns>
        Public Property ConsecutiveNoATP As Integer = 0
        Public Property TotalMolecules As Integer = 0

        ' [v2.0] 新增属性
        Public Property Age As Integer = 0
        Public Property DivisionCount As Integer = 0
        Public Property LastDivisionIteration As Long = 0

        ' [v3.0] 温度相关
        ''' <summary>细胞内当前温度（°C），受环境温度和代谢产热影响</summary>
        Public Property InternalTemperature As Double = 37.0

        ''' <summary>蛋白质活性修正因子（0.0-1.0），受温度和离子强度影响</summary>
        Public Property ProteinActivityFactor As Double = 1.0

        ' [v3.0] 渗透压相关
        ''' <summary>胞内离子强度（mM当量），影响渗透压</summary>
        Public ReadOnly Property InternalIonStrength As Double
            Get
                Return CalculateIonStrength()
            End Get
        End Property

        ''' <summary>
        ''' 细胞内基因总数（基因组+所有质粒）
        ''' </summary>
        Public ReadOnly Property TotalGeneCount As Integer
            Get
                Dim count = Genome.Genes.Count
                For Each p In Plasmids
                    count += p.Genes.Count
                Next
                Return count
            End Get
        End Property

        ''' <summary>
        ''' [v2.0] 基因组维护成本：每迭代消耗的ATP
        ''' </summary>
        Public ReadOnly Property GenomeMaintenanceCost As Integer
            Get
                Return CInt(Math.Ceiling(TotalGeneCount * 0.5))
            End Get
        End Property

        ''' <summary>胞内渗透压状态：-1=低渗, 0=等渗, 1=高渗</summary>
        Public Property OsmoticState As Integer = 0
        Public Property ColdShockMitigation As Double

        Public ReadOnly Property PH As Double
            Get
                Return PHHelper.EstimatePH(Me, temperatureC:=InternalTemperature)
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"[{ID.ToString}] Generation:{Generation}; ATP:{ATP}; Position: {Position}; Genome{{{Genome.ToString}}}"
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Function GetTotalGenes() As Dictionary(Of GeneOntology, Integer)
            Return Plasmids.JoinIterates(Genome) _
                .Select(Function(r) r.Genes) _
                .IteratesALL _
                .GroupBy(Function(g) g.FunctionOntology) _
                .ToDictionary(Function(g) g.Key,
                              Function(g)
                                  Return g.Count
                              End Function)
        End Function

        Public Function HasFunction(go As GeneOntology) As Boolean
            Return Proteins.ContainsKey(go) AndAlso Proteins(go) > 0
        End Function

        ''' <summary>
        ''' [v2.0] 获取细胞内指定分子的数量，不存在则返回0
        ''' </summary>
        Public Function GetMoleculeAmount(type As MoleculeType) As Integer
            If InternalMolecules.ContainsKey(type) Then
                Return InternalMolecules(type).Quantity
            Else
                Return 0
            End If
        End Function

        ''' <summary>
        ''' [v2.0] 设置细胞内指定分子的数量
        ''' </summary>
        Public Sub SetMoleculeAmount(type As MoleculeType, amount As Integer)
            If amount < 0 Then amount = 0
            If Not InternalMolecules.ContainsKey(type) Then
                InternalMolecules(type) = Molecule.EmptyModel(type)
            End If
            InternalMolecules(type).SetQuantity(amount)
        End Sub

        ''' <summary>
        ''' [v2.0] 向细胞内添加指定数量的分子
        ''' </summary>
        Public Sub AddMoleculeInternal(type As MoleculeType, amount As Integer)
            If Not InternalMolecules.ContainsKey(type) Then
                InternalMolecules(type) = Molecule.EmptyModel(type)
            End If
            InternalMolecules(type).AddQuantity(amount)
            If InternalMolecules(type).Quantity < 0 Then
                InternalMolecules(type).SetQuantity(0)
            End If
        End Sub

        ''' <summary>
        ''' [v3.0] 计算胞内离子强度
        ''' 离子强度 I = 0.5 * Σ(c_i * z_i^2)
        ''' 简化：每种离子按其价态平方加权
        ''' </summary>
        Private Function CalculateIonStrength() As Double
            Dim ionContributions As Double = 0.0

            ' 1价离子：Na+, K+, Cl-, H+
            ionContributions += GetMoleculeAmount(MoleculeType.SodiumIon) * 1.0
            ionContributions += GetMoleculeAmount(MoleculeType.PotassiumIon) * 1.0
            ionContributions += GetMoleculeAmount(MoleculeType.ChlorideIon) * 1.0
            ionContributions += GetMoleculeAmount(MoleculeType.HydrogenIon) * 1.0

            ' 2价离子：Ca2+, Mg2+, Fe2+, SO4 2-
            ionContributions += GetMoleculeAmount(MoleculeType.CalciumIon) * 4.0
            ionContributions += GetMoleculeAmount(MoleculeType.MagnesiumIon) * 4.0
            ionContributions += GetMoleculeAmount(MoleculeType.IronII) * 4.0
            ionContributions += GetMoleculeAmount(MoleculeType.Sulfate) * 4.0

            ' 3价离子：Fe3+
            ionContributions += GetMoleculeAmount(MoleculeType.IronIII) * 9.0

            ' 相容溶质不贡献离子强度（这正是其生物学意义）
            Return ionContributions * 0.5
        End Function

    End Class

End Namespace
