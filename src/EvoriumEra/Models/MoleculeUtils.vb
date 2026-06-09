Imports EvoriumEra.Models.Container

Namespace Models

    Public Class MoleculeUtils

        ReadOnly config As Configs
        ReadOnly env As NaturalEnvironment
        ReadOnly debug As Boolean

        Sub New(config As Configs, env As NaturalEnvironment, Optional debug As Boolean = False)
            Me.env = env
            Me.config = config
            Me.env.moleculeUtils = Me
            Me.debug = debug
        End Sub

        ''' <summary>
        ''' 向容器中添加或移除分子
        ''' </summary>
        ''' <param name="container">可以是Cell或Voxel</param>
        ''' <param name="moleculeType">分子类型</param>
        ''' <param name="amount">正数增加，负数减少</param>
        Public Sub AddMolecule(container As IVoxel, moleculeType As MoleculeType, amount As Integer)
            Select Case container.GetType()
                Case GetType(Cell)
                    AddToCell(DirectCast(container, Cell), moleculeType, amount)
                Case GetType(Voxel)
                    AddToVoxel(DirectCast(container, Voxel), moleculeType, amount)
                Case Else
                    Throw New ArgumentException("不支持的容器类型")
            End Select
        End Sub

        Private Sub AddToCell(cell As Cell, moleculeType As MoleculeType, amount As Integer)
            ' 初始化字典项
            If Not cell.InternalMolecules.ContainsKey(moleculeType) Then
                cell.InternalMolecules(moleculeType) = 0
            End If

            ' 更新分子数量
            cell.InternalMolecules(moleculeType) += amount

            ' 防止负数
            If cell.InternalMolecules(moleculeType) < 0 Then
                cell.InternalMolecules(moleculeType) = 0
            End If

            ' 更新总分子数
            cell.TotalMolecules += amount
            If cell.TotalMolecules < 0 Then cell.TotalMolecules = 0

            ' 检查容量限制（规则17）
            If cell.TotalMolecules > config.MaxCellContentCapacity Then
                ' 细胞破裂死亡
                env.recycler.LyseCell(cell, "total_molecules_reach_max_cell_content_capacity")
            End If
        End Sub

        Friend Sub AddToVoxel(voxel As Voxel, moleculeType As MoleculeType, amount As Integer)
            If Not voxel.ExternalMolecules.ContainsKey(moleculeType) Then
                voxel.ExternalMolecules(moleculeType) = 0
            End If

            voxel.ExternalMolecules(moleculeType) += amount
            If voxel.ExternalMolecules(moleculeType) < 0 Then
                voxel.ExternalMolecules(moleculeType) = 0
            End If
        End Sub
    End Class
End Namespace