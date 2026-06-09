Imports Microsoft.VisualBasic.Imaging

Namespace Models.Container

    Public Class Cell : Implements IVoxel

        Public Property ID As Guid = Guid.NewGuid()
        Public Property ParentID As Guid? = Nothing
        Public Property Generation As Integer = 0
        Public Property Position As SpatialIndex3D Implements IVoxel.Position
        Public Property Genome As Replicon
        Public Property Plasmids As List(Of Replicon) = New List(Of Replicon)
        Public Property InternalMolecules As New Dictionary(Of MoleculeType, Integer) Implements IVoxel.Molecules
        Public Property Proteins As New Dictionary(Of GeneOntology, Integer)
        Public Property HasCellWall As Boolean = False
        Public Property IsAlive As Boolean = True
        Public Property ATP As Integer = 200
        Public Property ConsecutiveNoATP As Integer = 0
        Public Property TotalMolecules As Integer = 0

        ' [v2.0] 新增属性
        Public Property Age As Integer = 0
        Public Property DivisionCount As Integer = 0

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

        Public Function HasFunction(go As GeneOntology) As Boolean
            Return Proteins.ContainsKey(go) AndAlso Proteins(go) > 0
        End Function

        ''' <summary>
        ''' [v2.0] 获取细胞内指定分子的数量，不存在则返回0
        ''' </summary>
        Public Function GetMoleculeAmount(type As MoleculeType) As Integer
            If InternalMolecules.ContainsKey(type) Then
                Return InternalMolecules(type)
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
                InternalMolecules(type) = 0
            End If
            InternalMolecules(type) = amount
        End Sub

        ''' <summary>
        ''' [v2.0] 向细胞内添加指定数量的分子
        ''' </summary>
        Public Sub AddMoleculeInternal(type As MoleculeType, amount As Integer)
            If Not InternalMolecules.ContainsKey(type) Then
                InternalMolecules(type) = 0
            End If
            InternalMolecules(type) += amount
            If InternalMolecules(type) < 0 Then InternalMolecules(type) = 0
        End Sub

    End Class

End Namespace
