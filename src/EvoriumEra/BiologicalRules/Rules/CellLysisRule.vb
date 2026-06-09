Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v2.0 新增] 细胞裂解规则
    ''' 
    ''' 死亡细胞裂解释放内容物到环境中，形成营养循环。
    ''' 这是"分解者"生态角色出现的前提，也是交叉喂养网络
    ''' 中物质回收的关键机制。
    ''' </summary>
    Public Class CellLysisRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New() ' 全局规则
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            If cell.IsAlive Then Return

            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)

            ' 释放所有内部分子到环境中
            For Each kvp In cell.InternalMolecules
                If kvp.Value > 0 Then
                    If Not voxel.ExternalMolecules.ContainsKey(kvp.Key) Then
                        voxel.ExternalMolecules(kvp.Key) = 0
                    End If
                    voxel.ExternalMolecules(kvp.Key) += kvp.Value
                End If
            Next

            ' DNA降解为核苷酸释放
            Dim dna = cell.GetMoleculeAmount(MoleculeType.DNA)
            If dna > 0 Then
                If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.Nucleotide) Then
                    voxel.ExternalMolecules(MoleculeType.Nucleotide) = 0
                End If
                voxel.ExternalMolecules(MoleculeType.Nucleotide) += dna * 3
            End If

            ' 蛋白质降解为氨基酸释放
            Dim totalProteins = cell.Proteins.Values.Sum()
            If totalProteins > 0 Then
                If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.AminoMixGluFamily) Then
                    voxel.ExternalMolecules(MoleculeType.AminoMixGluFamily) = 0
                End If
                voxel.ExternalMolecules(MoleculeType.AminoMixGluFamily) += totalProteins
            End If

            ' 清空细胞
            cell.InternalMolecules.Clear()
            cell.Proteins.Clear()
            cell.TotalMolecules = 0
            cell.ATP = 0
            voxel.Occupant = Nothing
        End Sub
    End Class
End Namespace
