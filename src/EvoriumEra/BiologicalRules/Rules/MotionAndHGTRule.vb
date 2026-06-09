Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports Microsoft.VisualBasic.Imaging
Imports rng = Microsoft.VisualBasic.Math.RandomExtensions

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v2.0 改进] 运动与水平基因转移规则
    ''' </summary>
    Public Class MotionAndHGTRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(GeneOntology.FlagellarMovement)
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            ' 鞭毛运动
            If cell.HasFunction(GeneOntology.FlagellarMovement) Then
                Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
                Dim neighbors = env.GetNeighbors(voxel).Where(Function(v) v.Occupant Is Nothing).ToList()

                If neighbors.Any() Then
                    ' [v2.0] 趋化性：优先向营养丰富的方向移动
                    Dim target = SelectChemotaxisTarget(neighbors, cell)

                    If target IsNot Nothing Then
                        If ConsumeBasicResources(cell) Then
                            ' 移动到新位置
                            voxel.Occupant = Nothing
                            target.Occupant = cell
                            cell.Position = target.Position
                        End If
                    End If
                End If
            End If

            ' HGT：相邻细胞间质粒交换
            TryHGT(cell, env)
        End Sub

        ''' <summary>
        ''' [v2.0] 趋化性：选择营养最丰富的邻居格子
        ''' </summary>
        Private Function SelectChemotaxisTarget(neighbors As List(Of Voxel), cell As Cell) As Voxel
            ' 计算每个邻居的营养评分
            Dim scored = neighbors.Select(Function(v)
                                              Dim score = 0
                                              score += v.GetMoleculeAmount(MoleculeType.Glucose) * 3
                                              score += v.GetMoleculeAmount(MoleculeType.Pyruvate) * 2
                                              score += v.GetMoleculeAmount(MoleculeType.Acetate) * 2
                                              score += v.GetMoleculeAmount(MoleculeType.CarbonSource)
                                              score += v.GetMoleculeAmount(MoleculeType.NitrogenSource)
                                              Return (v, score)
                                          End Function).ToList()

            ' 加权随机选择（营养越多的格子被选中概率越高）
            Dim totalScore = scored.Sum(Function(s) s.score + 1) ' +1避免全零
            Dim r = rng.NextDouble() * totalScore
            Dim cumulative = 0.0

            For Each item In scored
                cumulative += item.score + 1
                If r <= cumulative Then
                    Return item.v
                End If
            Next

            Return scored.Last.v
        End Function

        Private Sub TryHGT(cell As Cell, env As NaturalEnvironment)
            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
            Dim neighbors = env.GetNeighbors(voxel)

            For Each neighbor In neighbors
                If neighbor.Occupant IsNot Nothing AndAlso neighbor.Occupant.IsAlive Then
                    ' 5%概率发生HGT
                    If rng.NextDouble() < 0.05 Then
                        ExchangePlasmids(cell, neighbor.Occupant)
                    End If
                End If
            Next
        End Sub

        Private Sub ExchangePlasmids(cell1 As Cell, cell2 As Cell)
            If cell1.Plasmids.Any() AndAlso cell2.Plasmids.Any() Then
                Dim plasmid1 = cell1.Plasmids(rng.Next(cell1.Plasmids.Count))
                Dim plasmid2 = cell2.Plasmids(rng.Next(cell2.Plasmids.Count))

                cell1.Plasmids.Remove(plasmid1)
                cell2.Plasmids.Remove(plasmid2)
                cell1.Plasmids.Add(plasmid2)
                cell2.Plasmids.Add(plasmid1)
            ElseIf cell1.Plasmids.Any() Then
                ' 单向转移
                Dim plasmid = cell1.Plasmids(rng.Next(cell1.Plasmids.Count))
                cell1.Plasmids.Remove(plasmid)
                cell2.Plasmids.Add(plasmid)
            ElseIf cell2.Plasmids.Any() Then
                Dim plasmid = cell2.Plasmids(rng.Next(cell2.Plasmids.Count))
                cell2.Plasmids.Remove(plasmid)
                cell1.Plasmids.Add(plasmid)
            End If
        End Sub
    End Class
End Namespace
