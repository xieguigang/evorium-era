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
            Dim voxelProteins As ProteinMolecule = voxel.ExternalMolecules.TryGetValue(MoleculeType.Protein)
            Dim voxelDNAs As DNAMolecule = voxel.ExternalMolecules.TryGetValue(MoleculeType.DNA)

            If voxelDNAs Is Nothing Then
                ' 这些遗留在环境中的DNA片段会稳定的存在
                ' 从而可以被PCR扩增，以及做宏基因组检测等
                voxelDNAs = New DNAMolecule
                voxel.ExternalMolecules(MoleculeType.DNA) = voxelDNAs
            End If
            If voxelProteins Is Nothing Then
                voxelProteins = New ProteinMolecule
                voxel.ExternalMolecules(MoleculeType.Protein) = voxelProteins
            End If

            ' 将细胞内的代谢物释放到环境中
            For Each kvp In cell.InternalMolecules
                ' 跳过直接添加被吸收到内部的DNA片段列表
                If kvp.Key <> MoleculeType.DNA Then
                    env.moleculeUtils.AddToVoxel(voxel, kvp.Key, kvp.Value.Quantity)
                Else
                    ' 被吸收到细胞内的外源性DNA额外做处理
                    Dim externalDNAs As DNAMolecule = kvp.Value

                    For Each frag As Replicon In externalDNAs.DNAFragments
                        Call externalDNAs.Add(frag)
                    Next
                End If
            Next

            ' 将细胞内的蛋白质也释放到环境中
            For Each protKvp In cell.Proteins
                Call voxelProteins.add(protKvp.Key, protKvp.Value)
            Next

            ' 同时细胞内的DNA也会被释放到环境中
            Call voxelDNAs.Add(cell.Genome)

            For Each plasmid As Replicon In cell.Plasmids
                Call voxelDNAs.Add(plasmid)
            Next

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