Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v2.0 新增] 基因组维护成本规则
    ''' 
    ''' 每迭代每个基因消耗0.5个ATP作为基因组维护成本。
    ''' 这创造了基因组大小与代谢效率之间的权衡：
    ''' - 基因组越大，功能越多，但维护成本越高
    ''' - 基因组越小，维护成本低，但功能受限
    ''' 
    ''' 这是驱动代谢专化和基因组精简的关键机制。
    ''' 专化型细胞（只保留关键代谢基因）在资源受限时
    ''' 比通用型细胞（携带大量基因）更有生存优势。
    ''' </summary>
    Public Class GenomeMaintenanceRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New() ' 全局规则，不绑定特定基因功能
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            ' 计算总基因数
            Dim totalGenes = cell.Genome.Genes.Count

            For Each plasmid As Replicon In cell.Plasmids
                totalGenes += plasmid.Genes.Count
            Next

            ' 每个基因消耗0.5 ATP（四舍五入）
            Dim maintenanceCost = CInt(Math.Ceiling(totalGenes * 0.5))

            ' 消耗ATP
            cell.ATP = Math.Max(0, cell.ATP - maintenanceCost)
        End Sub
    End Class
End Namespace
