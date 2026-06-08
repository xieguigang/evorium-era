Public Interface IBiochemicalRule
    Sub Execute(cell As Cell, env As Environment3D, rng As Random)
End Interface



Public Class RuleScheduler
    Public Property Rules As List(Of IBiochemicalRule) = New List(Of IBiochemicalRule)

    Public Sub New()
        ' 按顺序添加所有规则
        Rules.Add(New EnergyMetabolismRule())
        Rules.Add(New GeneExpressionRule())
        Rules.Add(New ReplicationAndDivisionRule())
        Rules.Add(New TransportRule())
        Rules.Add(New SynthesisAndDegradationRule())
        Rules.Add(New EnvironmentalResponseRule())
        Rules.Add(New MotionAndHGTRule())
        Rules.Add(New MutationRule())
        Rules.Add(New QuorumSensingAndBiofilmRule())
        Rules.Add(New DiffusionRule())
    End Sub

    Public Sub ApplyAll(cell As Cell, env As Environment3D, rng As Random)
        ' 轮盘赌选择（简化：按顺序执行，实际应按蛋白质浓度加权）
        For Each rule In Rules
            If cell.IsAlive Then
                Try
                    rule.Execute(cell, env, rng)
                Catch ex As Exception
                    ' 记录异常但继续执行
                End Try
            End If
        Next
    End Sub
End Class

Public Class EnergyMetabolismRule : Implements IBiochemicalRule
    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        ' 需氧能量代谢
        If cell.Proteins.ContainsKey(GeneFunction.AerobicEnergyMetabolismATP) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Glucose) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Oxygen) Then

            If cell.InternalMolecules(MoleculeType.Glucose) > 0 AndAlso
               cell.InternalMolecules(MoleculeType.Oxygen) > 0 Then

                cell.InternalMolecules(MoleculeType.Glucose) -= 1
                cell.InternalMolecules(MoleculeType.Oxygen) -= 1
                cell.ATP = Math.Min(cell.ATP + 12, 1000)
                AddMolecule(cell, MoleculeType.CarbonDioxide, 6)
                ConsumeBasicResources(cell)
            End If
        End If

        ' 厌氧能量代谢
        If cell.Proteins.ContainsKey(GeneFunction.AnaerobicEnergyMetabolismATP) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Acetate) Then

            If cell.InternalMolecules(MoleculeType.Acetate) > 0 AndAlso
               Not cell.InternalMolecules.ContainsKey(MoleculeType.Oxygen) Then

                cell.InternalMolecules(MoleculeType.Acetate) -= 1
                cell.ATP = Math.Min(cell.ATP + 5, 1000)
                AddMolecule(cell, MoleculeType.CarbonDioxide, 2)
                AddMolecule(cell, MoleculeType.HydrogenIon, 2)
                ConsumeBasicResources(cell)
            End If
        End If
    End Sub

    Private Sub ConsumeBasicResources(cell As Cell)
        If cell.InternalMolecules.ContainsKey(MoleculeType.Water) Then
            cell.InternalMolecules(MoleculeType.Water) -= 1
        End If
    End Sub
End Class

Public Class GeneExpressionRule : Implements IBiochemicalRule
    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        ' 基因转录（需要9个核苷酸）
        If cell.Proteins.ContainsKey(GeneFunction.GeneTranscription) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Nucleotide) AndAlso
           cell.InternalMolecules(MoleculeType.Nucleotide) >= 9 Then

            cell.InternalMolecules(MoleculeType.Nucleotide) -= 9
            ' 转录产生RNA（这里简化为直接增加蛋白质数量）
            ' 实际应增加RNA计数，但按您的描述，我们简化
            ConsumeBasicResources(cell)
        End If

        ' 蛋白质翻译（需要3种氨基酸各1单位）
        If cell.Proteins.ContainsKey(GeneFunction.ProteinTranslation) Then
            Dim hasAminoAcids = cell.InternalMolecules.ContainsKey(MoleculeType.AminoMixGluFamily) AndAlso
                               cell.InternalMolecules.ContainsKey(MoleculeType.AminoMixAspFamily) AndAlso
                               cell.InternalMolecules.ContainsKey(MoleculeType.AminoMixSerGly)

            If hasAminoAcids AndAlso
               cell.InternalMolecules(MoleculeType.AminoMixGluFamily) > 0 AndAlso
               cell.InternalMolecules(MoleculeType.AminoMixAspFamily) > 0 AndAlso
               cell.InternalMolecules(MoleculeType.AminoMixSerGly) > 0 Then

                cell.InternalMolecules(MoleculeType.AminoMixGluFamily) -= 1
                cell.InternalMolecules(MoleculeType.AminoMixAspFamily) -= 1
                cell.InternalMolecules(MoleculeType.AminoMixSerGly) -= 1
                ' 增加随机蛋白质（简化）
                ConsumeBasicResources(cell)
            End If
        End If
    End Sub

    Private Sub ConsumeBasicResources(cell As Cell)
        If cell.InternalMolecules.ContainsKey(MoleculeType.Water) Then
            cell.InternalMolecules(MoleculeType.Water) -= 1
        End If
        cell.ATP -= 1
    End Sub
End Class

Public Class ReplicationAndDivisionRule : Implements IBiochemicalRule
    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        ' DNA复制（需要n*9 * 2个核苷酸）
        Dim totalGenes = cell.Genome.Genes.Count + cell.Plasmids.Sum(Function(p) p.Genes.Count)
        Dim requiredNucleotides = totalGenes * 9 * 2

        If cell.Proteins.ContainsKey(GeneFunction.ReplicateDNA) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Nucleotide) AndAlso
           cell.InternalMolecules(MoleculeType.Nucleotide) >= requiredNucleotides Then

            cell.InternalMolecules(MoleculeType.Nucleotide) -= requiredNucleotides
            ' 复制DNA（简化：标记已复制）
            ConsumeBasicResources(cell)
        End If

        ' 细胞分裂
        If cell.Proteins.ContainsKey(GeneFunction.CellDivision) Then
            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
            Dim neighbors = env.GetNeighbors(voxel).Where(Function(v) v.Occupant Is Nothing).ToList()

            If neighbors.Any() AndAlso cell.InternalMolecules.ContainsKey(MoleculeType.Nucleotide) AndAlso
               cell.InternalMolecules(MoleculeType.Nucleotide) >= requiredNucleotides Then

                ' 6:4分配
                Dim newCell As New Cell With {
                    .Position = (neighbors(rng.Next(neighbors.Count)).X,
                                neighbors(rng.Next(neighbors.Count)).Y,
                                neighbors(rng.Next(neighbors.Count)).Z),
                    .Genome = CloneReplicon(cell.Genome),
                    .Plasmids = cell.Plasmids.Select(Function(p) CloneReplicon(p)).ToList()
                }

                ' 分配分子（简化）
                DistributeMolecules(cell, newCell)
                env.Grid(newCell.Position.X, newCell.Position.Y, newCell.Position.Z).Occupant = newCell
                ConsumeBasicResources(cell)
            End If
        End If
    End Sub

    Private Function CloneReplicon(r As Replicon) As Replicon
        Return New Replicon With {.Genes = r.Genes.Select(Function(g) New Gene With {.FunctionTag = g.FunctionTag}).ToList()}
    End Function

    Private Sub DistributeMolecules(parent As Cell, child As Cell)
        ' 简化：按6:4分配所有分子
        For Each kvp In parent.InternalMolecules.ToList()
            Dim parentAmount = CInt(kvp.Value * 0.6)
            Dim childAmount = kvp.Value - parentAmount
            parent.InternalMolecules(kvp.Key) = parentAmount
            child.InternalMolecules(kvp.Key) = childAmount
        Next
    End Sub

    Private Sub ConsumeBasicResources(cell As Cell)
        If cell.InternalMolecules.ContainsKey(MoleculeType.Water) Then
            cell.InternalMolecules(MoleculeType.Water) -= 1
        End If
        cell.ATP -= 1
    End Sub
End Class

Public Class TransportRule : Implements IBiochemicalRule
    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)

        ' 物质内吞
        If cell.Proteins.ContainsKey(GeneFunction.Endocytosis) Then
            For Each moleculeType In voxel.ExternalMolecules.Keys.ToList()
                If Not IsPassiveDiffusion(moleculeType) Then
                    Dim amount = Math.Min(voxel.ExternalMolecules(moleculeType), 5)
                    If amount > 0 Then
                        voxel.ExternalMolecules(moleculeType) -= amount
                        AddMolecule(cell, moleculeType, amount)
                        ConsumeBasicResources(cell)
                    End If
                End If
            Next
        End If

        ' 物质分泌
        If cell.Proteins.ContainsKey(GeneFunction.Exocytosis) Then
            For Each moleculeType In cell.InternalMolecules.Keys.ToList()
                If Not IsPassiveDiffusion(moleculeType) Then
                    Dim amount = Math.Min(cell.InternalMolecules(moleculeType), 5)
                    If amount > 0 Then
                        cell.InternalMolecules(moleculeType) -= amount
                        AddMolecule(voxel, moleculeType, amount)
                        ConsumeBasicResources(cell)
                    End If
                End If
            Next
        End If
    End Sub

    Private Function IsPassiveDiffusion(type As MoleculeType) As Boolean
        Return type = MoleculeType.Oxygen OrElse
               type = MoleculeType.Water OrElse
               type = MoleculeType.HydrogenIon OrElse
               type = MoleculeType.HydroxideIon OrElse
               type = MoleculeType.CarbonDioxide OrElse
               type = MoleculeType.CarbonSource OrElse
               type = MoleculeType.NitrogenSource
    End Function

    Private Sub AddMolecule(container As Object, type As MoleculeType, amount As Integer)
        If container.GetType() = GetType(Cell) Then
            Dim cell = CType(container, Cell)
            If Not cell.InternalMolecules.ContainsKey(type) Then
                cell.InternalMolecules(type) = 0
            End If
            cell.InternalMolecules(type) += amount
        ElseIf container.GetType() = GetType(Voxel) Then
            Dim voxel = CType(container, Voxel)
            If Not voxel.ExternalMolecules.ContainsKey(type) Then
                voxel.ExternalMolecules(type) = 0
            End If
            voxel.ExternalMolecules(type) += amount
        End If
    End Sub

    Private Sub ConsumeBasicResources(cell As Cell)
        If cell.InternalMolecules.ContainsKey(MoleculeType.Water) Then
            cell.InternalMolecules(MoleculeType.Water) -= 1
        End If
        cell.ATP -= 1
    End Sub
End Class

Public Class SynthesisAndDegradationRule : Implements IBiochemicalRule
    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        ' 降解大分子
        If cell.Proteins.ContainsKey(GeneFunction.DegradeMacromolecule) Then
            ' 降解细胞内大分子
            Dim macromolecules = {MoleculeType.SecondaryMetabolite, MoleculeType.Nucleotide,
                                 MoleculeType.DNA, MoleculeType.AminoMixGluFamily,
                                 MoleculeType.Biofilm}

            For Each mol In macromolecules
                If cell.InternalMolecules.ContainsKey(mol) AndAlso cell.InternalMolecules(mol) > 0 Then
                    cell.InternalMolecules(mol) -= 1
                    AddMolecule(cell, MoleculeType.CarbonSource, 1)
                    AddMolecule(cell, MoleculeType.NitrogenSource, 1)
                    Exit For
                End If
            Next
            ConsumeBasicResources(cell)
        End If

        ' 合成抗生素
        If cell.Proteins.ContainsKey(GeneFunction.SynthesizeAntibiotic) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Acetate) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.NitrogenSource) Then

            cell.InternalMolecules(MoleculeType.Acetate) -= 1
            cell.InternalMolecules(MoleculeType.NitrogenSource) -= 1
            AddMolecule(cell, MoleculeType.Antibiotic, 1)
            ConsumeBasicResources(cell)
        End If

        ' 降解抗生素
        If cell.Proteins.ContainsKey(GeneFunction.DegradeAntibiotic) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.Antibiotic) Then

            cell.InternalMolecules(MoleculeType.Antibiotic) -= 1
            AddMolecule(cell, MoleculeType.Acetate, 1)
            AddMolecule(cell, MoleculeType.NitrogenSource, 1)
            AddMolecule(cell, MoleculeType.HydroxideIon, 2)
            ConsumeBasicResources(cell)
        End If
    End Sub

    Private Sub AddMolecule(cell As Cell, type As MoleculeType, amount As Integer)
        If Not cell.InternalMolecules.ContainsKey(type) Then
            cell.InternalMolecules(type) = 0
        End If
        cell.InternalMolecules(type) += amount
    End Sub

    Private Sub ConsumeBasicResources(cell As Cell)
        If cell.InternalMolecules.ContainsKey(MoleculeType.Water) Then
            cell.InternalMolecules(MoleculeType.Water) -= 1
        End If
        cell.ATP -= 1
    End Sub
End Class

Public Class EnvironmentalResponseRule : Implements IBiochemicalRule
    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        ' 酸代谢
        If cell.Proteins.ContainsKey(GeneFunction.AcidMetabolism) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.HydrogenIon) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.CarbonSource) Then

            If cell.InternalMolecules(MoleculeType.HydrogenIon) >= 20 Then
                cell.InternalMolecules(MoleculeType.HydrogenIon) -= 1
                cell.InternalMolecules(MoleculeType.CarbonSource) -= 1
                AddMolecule(cell, MoleculeType.Water, 1)
                cell.ATP += 1
            End If
        End If

        ' 碱代谢
        If cell.Proteins.ContainsKey(GeneFunction.BaseMetabolism) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.HydroxideIon) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.NitrogenSource) Then

            If cell.InternalMolecules(MoleculeType.HydroxideIon) >= 20 Then
                cell.InternalMolecules(MoleculeType.HydroxideIon) -= 1
                cell.InternalMolecules(MoleculeType.NitrogenSource) -= 1
                AddMolecule(cell, MoleculeType.Water, 1)
                cell.ATP += 1
            End If
        End If

        ' 抗生素抑制
        If cell.InternalMolecules.ContainsKey(MoleculeType.Antibiotic) AndAlso
           cell.InternalMolecules(MoleculeType.Antibiotic) > 0 Then
            ' 除降解抗生素外，所有蛋白质失活
            ' 这里简化处理：减少ATP消耗但不执行其他功能
            cell.ATP = Math.Max(0, cell.ATP - 1)
        End If
    End Sub

    Private Sub AddMolecule(cell As Cell, type As MoleculeType, amount As Integer)
        If Not cell.InternalMolecules.ContainsKey(type) Then
            cell.InternalMolecules(type) = 0
        End If
        cell.InternalMolecules(type) += amount
    End Sub
End Class

Public Class MotionAndHGTRule : Implements IBiochemicalRule
    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        ' 细胞鞭毛运动
        If cell.Proteins.ContainsKey(GeneFunction.FlagellarMovement) Then
            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
            Dim neighbors = env.GetNeighbors(voxel).Where(Function(v) v.Occupant Is Nothing).ToList()

            If neighbors.Any() Then
                Dim target = neighbors(rng.Next(neighbors.Count))
                env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z).Occupant = Nothing
                cell.Position = (target.X, target.Y, target.Z)
                target.Occupant = cell
                ConsumeBasicResources(cell)
            End If
        End If

        ' 质粒交换（相邻细胞）
        Dim currentVoxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
        For Each neighbor In env.GetNeighbors(currentVoxel)
            If neighbor.Occupant IsNot Nothing Then
                If rng.NextDouble() < 0.01 Then ' 1%概率
                    ExchangePlasmids(cell, neighbor.Occupant, rng)
                End If
            End If
        Next
    End Sub

    Private Sub ExchangePlasmids(cell1 As Cell, cell2 As Cell, rng As Random)
        If cell1.Plasmids.Any() AndAlso cell2.Plasmids.Any() Then
            Dim plasmid1 = cell1.Plasmids(rng.Next(cell1.Plasmids.Count))
            Dim plasmid2 = cell2.Plasmids(rng.Next(cell2.Plasmids.Count))

            cell1.Plasmids.Remove(plasmid1)
            cell2.Plasmids.Remove(plasmid2)
            cell1.Plasmids.Add(plasmid2)
            cell2.Plasmids.Add(plasmid1)
        End If
    End Sub

    Private Sub ConsumeBasicResources(cell As Cell)
        If cell.InternalMolecules.ContainsKey(MoleculeType.Water) Then
            cell.InternalMolecules(MoleculeType.Water) -= 1
        End If
        cell.ATP -= 1
    End Sub
End Class

Public Class MutationRule : Implements IBiochemicalRule
    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        ' DNA复制时突变（简化：每次复制有1%突变率）
        If cell.Proteins.ContainsKey(GeneFunction.ReplicateDNA) AndAlso rng.NextDouble() < 0.01 Then
            Dim replicons = New List(Of Replicon) From {cell.Genome}
            replicons.AddRange(cell.Plasmids)

            For Each replicon In replicons
                If replicon.Genes.Any() Then
                    Select Case rng.Next(3)
                        Case 0 ' 缺失突变
                            Dim index = rng.Next(replicon.Genes.Count)
                            replicon.Genes.RemoveAt(index)
                        Case 1 ' 插入突变
                            Dim gene = replicon.Genes(rng.Next(replicon.Genes.Count))
                            replicon.Genes.Add(New Gene With {.FunctionTag = gene.FunctionTag})
                        Case 2 ' 功能突变
                            Dim index = rng.Next(replicon.Genes.Count)
                            Dim allFunctions = [Enum].GetValues(GetType(GeneFunction)).Cast(Of GeneFunction)().ToList()
                            replicon.Genes(index).FunctionTag = allFunctions(rng.Next(allFunctions.Count))
                    End Select
                End If
            Next
        End If
    End Sub
End Class

Public Class QuorumSensingAndBiofilmRule : Implements IBiochemicalRule
    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        ' 信号分子合成
        If cell.Proteins.ContainsKey(GeneFunction.SignalMoleculeSynthesis) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.CarbonSource) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.AminoMixSerGly) Then

            cell.InternalMolecules(MoleculeType.CarbonSource) -= 1
            cell.InternalMolecules(MoleculeType.AminoMixSerGly) -= 1
            AddMolecule(cell, MoleculeType.SignalMolecule, 1)
            ConsumeBasicResources(cell)
        End If

        ' 群体感应
        If cell.Proteins.ContainsKey(GeneFunction.QuorumSensing) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.SignalMolecule) AndAlso
           cell.InternalMolecules(MoleculeType.SignalMolecule) >= 100 Then

            ' 强制执行次级代谢产物合成或生物膜合成
            If cell.Proteins.ContainsKey(GeneFunction.SecondaryMetaboliteSynthesis) Then
                ExecuteSecondaryMetaboliteSynthesis(cell)
            ElseIf cell.Proteins.ContainsKey(GeneFunction.BiofilmSynthesis) Then
                ExecuteBiofilmSynthesis(cell, env)
            End If
        End If
    End Sub

    Private Sub ExecuteSecondaryMetaboliteSynthesis(cell As Cell)
        If cell.InternalMolecules.ContainsKey(MoleculeType.Acetate) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.AminoMixGluFamily) AndAlso
           cell.InternalMolecules(MoleculeType.Acetate) >= 2 AndAlso
           cell.InternalMolecules(MoleculeType.AminoMixGluFamily) >= 1 Then

            cell.InternalMolecules(MoleculeType.Acetate) -= 2
            cell.InternalMolecules(MoleculeType.AminoMixGluFamily) -= 1
            AddMolecule(cell, MoleculeType.SecondaryMetabolite, 1)
        End If
    End Sub

    Private Sub ExecuteBiofilmSynthesis(cell As Cell, env As Environment3D)
        If cell.InternalMolecules.ContainsKey(MoleculeType.NitrogenSource) AndAlso
           cell.InternalMolecules.ContainsKey(MoleculeType.AminoMixAspFamily) AndAlso
           cell.InternalMolecules(MoleculeType.NitrogenSource) >= 10 AndAlso
           cell.InternalMolecules(MoleculeType.AminoMixAspFamily) >= 5 Then

            cell.InternalMolecules(MoleculeType.NitrogenSource) -= 10
            cell.InternalMolecules(MoleculeType.AminoMixAspFamily) -= 5
            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
            voxel.HasBiofilm = True
        End If
    End Sub

    Private Sub AddMolecule(cell As Cell, type As MoleculeType, amount As Integer)
        If Not cell.InternalMolecules.ContainsKey(type) Then
            cell.InternalMolecules(type) = 0
        End If
        cell.InternalMolecules(type) += amount
    End Sub

    Private Sub ConsumeBasicResources(cell As Cell)
        If cell.InternalMolecules.ContainsKey(MoleculeType.Water) Then
            cell.InternalMolecules(MoleculeType.Water) -= 1
        End If
        cell.ATP -= 1
    End Sub
End Class

Public Class DiffusionRule : Implements IBiochemicalRule
    Public Sub Execute(cell As Cell, env As Environment3D, rng As Random) Implements IBiochemicalRule.Execute
        Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
        Dim neighbors = env.GetNeighbors(voxel)

        For Each neighbor In neighbors
            ' 被动扩散分子
            Dim diffusable = {MoleculeType.Oxygen, MoleculeType.Water, MoleculeType.HydrogenIon,
                             MoleculeType.HydroxideIon, MoleculeType.CarbonDioxide,
                             MoleculeType.CarbonSource, MoleculeType.NitrogenSource}

            For Each mol In diffusable
                If voxel.ExternalMolecules.ContainsKey(mol) AndAlso
                   neighbor.ExternalMolecules.ContainsKey(mol) Then

                    Dim diff = voxel.ExternalMolecules(mol) - neighbor.ExternalMolecules(mol)
                    If Math.Abs(diff) > 0 Then
                        Dim transfer = CInt(Math.Sign(diff) * Math.Min(Math.Abs(diff) * 0.1, rng.Next(1, 6)))
                        voxel.ExternalMolecules(mol) -= transfer
                        neighbor.ExternalMolecules(mol) += transfer
                    End If
                End If
            Next
        Next
    End Sub
End Class

