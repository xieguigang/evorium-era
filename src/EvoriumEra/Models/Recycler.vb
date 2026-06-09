Imports EvoriumEra.Models.Container

Namespace Models

    ''' <summary>
    ''' [v2.0 新增] 细胞裂解规则
    ''' 
    ''' 死亡细胞裂解释放内容物到环境中，形成营养循环。
    ''' 这是"分解者"生态角色出现的前提，也是交叉喂养网络
    ''' 中物质回收的关键机制。
    ''' </summary>
    Public Class Recycler

        ReadOnly config As Configs
        ReadOnly env As NaturalEnvironment
        ReadOnly debug As Boolean

        Sub New(config As Configs, env As NaturalEnvironment, Optional debug As Boolean = False)
            Me.env = env
            Me.config = config
            Me.env.recycler = Me
            Me.debug = debug
        End Sub

        ''' <summary>
        ''' 杀死细胞，并将细胞内所有物质释放到当前格子
        ''' </summary>
        ''' <param name="cell"></param>
        Public Sub LyseCell(cell As Cell, reason As String)
            Dim voxel = env(cell.Position.X, cell.Position.Y, cell.Position.Z)

            ' 将细胞内的代谢物释放到环境中
            For Each kvp In cell.InternalMolecules
                env.moleculeUtils.AddToVoxel(voxel, kvp.Key, kvp.Value.Quantity)
            Next

            ' 将细胞内的蛋白质也释放到环境中
            ' DNA降解为核苷酸释放
            Dim dna = cell.GetMoleculeAmount(MoleculeType.DNA)

            If dna > 0 Then
                voxel.ExternalMolecules(MoleculeType.Nucleotide).Quantity += dna * 3
            End If

            ' 蛋白质降解为氨基酸释放
            Dim totalProteins = cell.Proteins.Values.Sum()

            If totalProteins > 0 Then
                voxel.ExternalMolecules(MoleculeType.AminoMixGluFamily).Quantity += totalProteins
            End If

            ' 清空细胞
            cell.IsAlive = False
            cell.InternalMolecules.Clear()
            cell.TotalMolecules = 0
            cell.ConsecutiveNoATP = Integer.MaxValue
            cell.Proteins.Clear()
            cell.ATP = 0

            voxel.Occupant = Nothing

            If debug Then
                Call VBDebugger.warning($"[lyse_cell, {reason}] {cell.ToString}")
            End If
        End Sub
    End Class
End Namespace