Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports Microsoft.VisualBasic.Imaging
Imports rng = Microsoft.VisualBasic.Math.RandomExtensions

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v2.0 改进] 复制与分裂规则
    ''' 
    ''' 改进：
    ''' 1. 增加最低ATP阈值（300）和最低分子总量（500）
    ''' 2. 年龄越大分裂概率越低
    ''' 3. 分裂后蛋白质按6:4分配
    ''' </summary>
    Public Class ReplicationAndDivisionRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(GeneOntology.CellDivision)
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            ' 检查分裂条件
            If Not CanDivide(cell) Then Return

            ' DNA复制
            Dim totalGenes = cell.Genome.Genes.Count + cell.Plasmids.Sum(Function(p) p.Genes.Count)
            Dim requiredNucleotides = totalGenes * 9 * 2

            If Not cell.HasFunction(GeneOntology.ReplicateDNA) Then Return
            If cell.GetMoleculeAmount(MoleculeType.Nucleotide) < requiredNucleotides Then Return
            If cell.ATP < 300 Then Return
            If cell.TotalMolecules < 500 Then Return

            ' 年龄影响分裂概率
            Dim divisionProb = Math.Max(0.1, 1.0 - cell.Age * 0.02)
            If rng.NextDouble() > divisionProb Then Return

            ' 消耗核苷酸和ATP
            cell.AddMoleculeInternal(MoleculeType.Nucleotide, -requiredNucleotides)
            cell.ATP -= 50

            ' 寻找空位
            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
            Dim neighbors = env.GetNeighbors(voxel).Where(Function(v) v.Occupant Is Nothing).ToList()

            If Not neighbors.Any() Then Return

            Dim target = neighbors(rng.Next(neighbors.Count))

            ' 创建子细胞
            Dim child = CreateDaughterCell(cell)
            child.Position = target.Position

            ' 分配分子
            DistributeMolecules(cell, child)

            ' 分配蛋白质
            DistributeProteins(cell, child)

            ' 放置子细胞
            target.Occupant = child

            ' 记录分裂
            cell.DivisionCount += 1
            cell.Age = 0
        End Sub

        Private Function CanDivide(cell As Cell) As Boolean
            Return cell.IsAlive AndAlso
                   cell.ATP >= 300 AndAlso
                   cell.TotalMolecules >= 500 AndAlso
                   cell.HasFunction(GeneOntology.CellDivision) AndAlso
                   cell.HasFunction(GeneOntology.ReplicateDNA)
        End Function

        Private Function CreateDaughterCell(parent As Cell) As Cell
            Return New Cell With {
                .ID = Guid.NewGuid(),
                .ParentID = parent.ID,
                .Generation = parent.Generation + 1,
                .Genome = New Replicon With {
                    .Genes = parent.Genome.Genes.Select(Function(g) New Gene With {.FunctionOntology = g.FunctionOntology}).ToList()
                },
                .Plasmids = parent.Plasmids.Select(Function(p) New Replicon With {
                    .IsPlasmid = True,
                    .Genes = p.Genes.Select(Function(g) New Gene With {.FunctionOntology = g.FunctionOntology}).ToList()
                }).ToList(),
                .ATP = 100,
                .Age = 0,
                .DivisionCount = 0
            }
        End Function

        Private Sub DistributeMolecules(parent As Cell, child As Cell)
            For Each kvp In parent.InternalMolecules.ToList()
                Dim parentAmount = CInt(kvp.Value * 0.6)
                Dim childAmount = kvp.Value - parentAmount
                parent.InternalMolecules(kvp.Key) = parentAmount
                child.InternalMolecules(kvp.Key) = childAmount
            Next
        End Sub

        Private Sub DistributeProteins(parent As Cell, child As Cell)
            For Each kvp In parent.Proteins.ToList()
                Dim parentAmount = CInt(kvp.Value * 0.6)
                Dim childAmount = kvp.Value - parentAmount
                parent.Proteins(kvp.Key) = parentAmount
                child.Proteins(kvp.Key) = childAmount
            Next
        End Sub
    End Class
End Namespace
