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
        Public Property TotalMolecules As Integer
        Public Property ATP As Integer

        ' 遗传信息
        Public Property GenomeSize As Integer
        Public Property PlasmidCount As Integer
        Public Property GeneCounts As Dictionary(Of GeneOntology, Integer)

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