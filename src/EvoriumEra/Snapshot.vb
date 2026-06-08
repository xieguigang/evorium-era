Imports System.Text

<Serializable>
Public Class Snapshot
    ' 元数据
    Public Property Iteration As Long
    Public Property Timestamp As DateTime
    Public Property SimulationTime As TimeSpan

    ' 环境快照
    Public Property EnvironmentDimensions As (Width As Integer, Height As Integer, Depth As Integer)
    Public Property Voxels As List(Of VoxelSnapshot)

    ' 细胞快照
    Public Property Cells As List(Of CellSnapshot)

    ' 统计汇总
    Public Property TotalLivingCells As Integer
    Public Property TotalDeadCells As Integer
    Public Property TotalMoleculesInSystem As Long
    Public Property AverageATPCells As Double

    ' 快速访问索引
    <NonSerialized>
    Private _voxelIndex As Dictionary(Of (Integer, Integer, Integer), VoxelSnapshot)

    Public Function GetVoxel(x As Integer, y As Integer, z As Integer) As VoxelSnapshot
        If _voxelIndex Is Nothing Then
            _voxelIndex = New Dictionary(Of (Integer, Integer, Integer), VoxelSnapshot)
            For Each v In Voxels
                _voxelIndex((v.X, v.Y, v.Z)) = v
            Next
        End If

        Dim key = (x, y, z)
        If _voxelIndex.ContainsKey(key) Then
            Return _voxelIndex(key)
        End If
        Return Nothing
    End Function
End Class

Public Class CellSnapshot
    ' 标识信息
    Public Property ID As Guid
    Public Property ParentID As Guid? ' 母细胞ID（用于追踪谱系）

    ' 位置与状态
    Public Property Position As (X As Integer, Y As Integer, Z As Integer)
    Public Property IsAlive As Boolean
    Public Property HasCellWall As Boolean

    ' 分子库存
    Public Property InternalMolecules As Dictionary(Of MoleculeType, Integer)
    Public Property TotalMolecules As Integer
    Public Property ATP As Integer

    ' 遗传信息
    Public Property GenomeSize As Integer
    Public Property PlasmidCount As Integer
    Public Property GeneCounts As Dictionary(Of GeneFunction, Integer)

    ' 统计指标
    Public Property Age As Integer ' 存活了多少个迭代
    Public Property DivisionCount As Integer ' 分裂次数

    ' 计算属性
    Public ReadOnly Property EnergyCharge As Double
        Get
            If ATP <= 0 Then Return 0
            Return Math.Min(1.0, ATP / 1000.0)
        End Get
    End Property
End Class

Public Class VoxelSnapshot
    ' 位置信息
    Public Property X As Integer
    Public Property Y As Integer
    Public Property Z As Integer

    ' 外部分子浓度
    Public Property ExternalMolecules As Dictionary(Of MoleculeType, Integer)

    ' 物理状态
    Public Property HasBiofilm As Boolean

    ' 占据情况
    Public Property OccupantCellId As Guid?
    Public Property OccupantCellAlive As Boolean?

    ' 统计信息
    Public Property TotalMolecules As Integer
    Public Property MoleculeDensity As Double

    ' 时间戳
    Public Property SnapshotTime As DateTime

    ' 转换为可读字符串（用于调试）
    Public Overrides Function ToString() As String
        Dim sb As New StringBuilder()
        sb.AppendLine($"Voxel({X},{Y},{Z})")
        sb.AppendLine($"  Biofilm: {HasBiofilm}")
        sb.AppendLine($"  Occupied: {If(OccupantCellId.HasValue, OccupantCellId.ToString(), "Empty")}")
        sb.AppendLine($"  Total Molecules: {TotalMolecules}")

        If ExternalMolecules IsNot Nothing Then
            For Each kvp In ExternalMolecules.Where(Function(kv) kv.Value > 0)
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}")
            Next
        End If

        Return sb.ToString()
    End Function
End Class