Imports EvoriumEra.Models
Imports Microsoft.VisualBasic.Imaging

Namespace Data

    Public Class CellSnapshot

        ''' <summary>
        ''' 标识信息
        ''' </summary>
        ''' <returns></returns>
        Public Property ID As Guid
        ''' <summary>
        ''' 母细胞ID（用于追踪谱系）
        ''' </summary>
        ''' <returns></returns>
        Public Property ParentID As Guid?
        Public Property Generation As Integer
        ' 位置与状态
        Public Property Position As SpatialIndex3D
        Public Property IsAlive As Boolean
        Public Property HasCellWall As Boolean

        ' 分子库存
        Public Property InternalMolecules As Dictionary(Of MoleculeType, Integer)
        Public Property Proteins As Dictionary(Of GeneOntology, Integer)
        Public Property TotalMolecules As Integer
        Public Property ATP As Integer

        ' 遗传信息
        Public Property GenomeSize As Integer
        Public Property PlasmidCount As Integer
        Public Property GeneCounts As Dictionary(Of GeneOntology, Integer)

        Public Property Genome As Replicon
        Public Property Plasmids As Replicon()

        ''' <summary>
        ''' 存活了多少个迭代
        ''' </summary>
        ''' <returns></returns>
        Public Property Age As Integer
        ''' <summary>
        ''' 分裂次数
        ''' </summary>
        ''' <returns></returns>
        Public Property DivisionCount As Integer
        Public Property ColdShockMitigation As Double
        ''' <summary>胞内渗透压状态：-1=低渗, 0=等渗, 1=高渗</summary>
        Public Property OsmoticState As Integer = 0
        Public Property InternalIonStrength As Double

        ''' <summary>蛋白质活性修正因子（0.0-1.0），受温度和离子强度影响</summary>
        Public Property ProteinActivityFactor As Double = 1.0

        ''' <summary>
        ''' 计算属性
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property EnergyCharge As Double
            Get
                If ATP <= 0 Then Return 0
                Return Math.Min(1.0, ATP / 1000.0)
            End Get
        End Property
    End Class
End Namespace