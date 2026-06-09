Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports rng = Microsoft.VisualBasic.Math.RandomExtensions

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v4.0 新增] 外源DNA整合规则
    ''' 
    ''' 规则逻辑：
    ''' 1. 细胞裂解释放的DNA片段（基因组DNA和质粒DNA）稳定存在于环境中
    ''' 2. 其他细胞可以通过内吞（Endocytosis）吸收环境中的外源DNA片段
    ''' 3. 吸收后的外源DNA片段在细胞内有两个命运：
    '''    a) 被整合到基因组上（需要DNAIntegration功能）—— 水平基因转移
    '''    b) 被降解为核苷酸（需要NucleicAcidDegradation功能）—— 营养回收
    ''' 4. 环境中的降解酶（具有NucleicAcidDegradation功能的活性蛋白质）
    '''    也可以在环境中将DNA片段降解为核苷酸（由EnvironmentalDNADegradationRule处理）
    ''' 
    ''' 本规则为全局生化规则（IBiochemicalRule，不绑定特定基因功能），
    ''' 在每个迭代的细胞阶段对每个活细胞执行。
    ''' 
    ''' 生物学意义：
    ''' - 自然转化（Natural Transformation）：细菌摄取环境中的游离DNA
    ''' - 水平基因转移（HGT）：外源基因整合到受体细胞基因组
    ''' - 营养回收：DNA作为磷源和氮源被降解利用
    ''' - 基因组扩增：通过整合外源基因获得新功能
    ''' </summary>
    Public Class ExogenousDNAIntegrationRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New() ' 全局规则，不绑定特定基因功能
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
            Dim config = env.configs

            ' ===== 阶段1：细胞内吞环境中的外源DNA片段 =====
            ' 只有具有Endocytosis功能的细胞才能摄取外源DNA
            If cell.HasFunction(GeneOntology.Endocytosis) Then
                IngestExogenousDNA(cell, voxel, env, config)
            End If

            ' ===== 阶段2：处理细胞内已存在的外源DNA片段 =====
            ProcessInternalExogenousDNA(cell, config)
        End Sub

        ''' <summary>
        ''' 阶段1：细胞通过内吞摄取环境中的外源DNA片段
        ''' 
        ''' 只有具有Endocytosis功能的细胞才能摄取外源DNA。
        ''' 每次内吞最多摄取1个DNA片段（Replicon）。
        ''' 摄取概率受细胞ATP水平影响：ATP越充足，摄取概率越高。
        ''' </summary>
        Private Sub IngestExogenousDNA(cell As Cell, voxel As Voxel, env As NaturalEnvironment, config As Configs)
            ' 检查环境中是否有DNA
            Dim dnaMol = voxel.ExternalMolecules.TryGetValue(MoleculeType.DNA)
            If dnaMol Is Nothing Then Return

            Dim envDNA As DNAMolecule = dnaMol
            If envDNA.DNAFragments.Count = 0 Then Return

            ' 内吞消耗ATP
            If cell.ATP < config.DNAIngestionATPCost Then Return

            ' 摄取概率：基础概率 + ATP加成
            Dim ingestProb = config.DNAIngestionBaseProb + (cell.ATP / 1000.0) * 0.2
            If rng.NextDouble() > ingestProb Then Return

            ' 随机选择一个DNA片段摄取
            Dim fragmentIndex = rng.Next(envDNA.DNAFragments.Count)
            Dim fragment = envDNA.DNAFragments(fragmentIndex)

            ' 从环境中移除该DNA片段
            envDNA.DNAFragments.RemoveAt(fragmentIndex)

            ' 将外源DNA添加到细胞内部
            ' 使用DNAMolecule存储在InternalMolecules中
            Dim cellDNA As DNAMolecule = cell.InternalMolecules.TryGetValue(MoleculeType.DNA)
            If cellDNA Is Nothing Then
                cellDNA = New DNAMolecule()
                cell.InternalMolecules(MoleculeType.DNA) = cellDNA
            End If

            cellDNA.Add(fragment)

            ' 内吞消耗ATP
            cell.ATP -= config.DNAIngestionATPCost
        End Sub

        ''' <summary>
        ''' 阶段2：处理细胞内已存在的外源DNA片段
        ''' 
        ''' 外源DNA在细胞内有两个命运：
        ''' a) 整合到基因组（需要DNAIntegration功能，消耗ATP）
        '''    - 整合概率受基因组大小限制（基因组越大，整合概率越低）
        '''    - 质粒DNA优先整合为质粒，基因组DNA的基因可整合到基因组
        ''' b) 降解为核苷酸（需要NucleicAcidDegradation功能）
        '''    - 降解产生核苷酸和磷酸盐，作为营养回收
        ''' 
        ''' 如果细胞既没有DNAIntegration也没有NucleicAcidDegradation功能，
        ''' 外源DNA会暂时留在细胞内，但每迭代有概率自发降解。
        ''' </summary>
        Private Sub ProcessInternalExogenousDNA(cell As Cell, config As Configs)
            Dim cellDNA As DNAMolecule = cell.InternalMolecules.TryGetValue(MoleculeType.DNA)
            If cellDNA Is Nothing Then Return
            If cellDNA.DNAFragments.Count = 0 Then Return

            ' 处理每个外源DNA片段
            Dim processed As New List(Of Replicon)

            For Each fragment In cellDNA.DNAFragments.ToList()
                Dim wasProcessed = False

                ' 尝试整合到基因组
                If cell.HasFunction(GeneOntology.DNAIntegration) Then
                    If TryIntegrateDNA(cell, fragment, config) Then
                        processed.Add(fragment)
                        wasProcessed = True
                    End If
                End If

                ' 如果整合失败或没有整合功能，尝试降解
                If Not wasProcessed AndAlso cell.HasFunction(GeneOntology.NucleicAcidDegradation) Then
                    DegradeDNAToNucleotides(cell, fragment)
                    processed.Add(fragment)
                    wasProcessed = True
                End If

                ' 如果既不能整合也不能降解，有概率自发降解
                If Not wasProcessed Then
                    If rng.NextDouble() < config.SpontaneousDNADegradationProb Then
                        DegradeDNAToNucleotides(cell, fragment)
                        processed.Add(fragment)
                    End If
                End If
            Next

            ' 从细胞内移除已处理的DNA片段
            For Each frag In processed
                cellDNA.DNAFragments.Remove(frag)
            Next
        End Sub

        ''' <summary>
        ''' 尝试将外源DNA片段整合到细胞基因组中
        ''' 
        ''' 整合逻辑：
        ''' - 质粒DNA → 整合为细胞的质粒
        ''' - 基因组DNA → 将基因添加到细胞基因组末尾
        ''' - 整合概率受基因组大小限制：基因组越大，整合概率越低
        ''' - 整合消耗ATP（每个基因消耗配置的ATP量）
        ''' </summary>
        ''' <returns>True表示整合成功</returns>
        Private Function TryIntegrateDNA(cell As Cell, fragment As Replicon, config As Configs) As Boolean
            ' 计算整合概率：基因组越大，整合概率越低
            ' 基础概率由配置决定，每多一个基因降低2%
            Dim totalGenes = cell.TotalGeneCount
            Dim integrationProb = Math.Max(0.05, config.DNAIntegrationBaseProb - totalGenes * 0.02)

            If rng.NextDouble() > integrationProb Then Return False

            ' 计算整合成本：每个基因消耗配置的ATP量
            Dim integrationCost = fragment.Genes.Count * config.DNAIntegrationATPCostPerGene
            If cell.ATP < integrationCost Then Return False

            If fragment.IsPlasmid Then
                ' 质粒DNA → 整合为细胞的质粒
                cell.Plasmids.Add(fragment.Clone())
                cell.ATP -= integrationCost
            Else
                ' 基因组DNA → 将基因添加到细胞基因组末尾
                ' 限制基因组大小
                If cell.Genome.Genes.Count + fragment.Genes.Count > config.MaxGenomeGeneCount Then Return False

                ' 将外源基因添加到基因组末尾
                For Each gene In fragment.Genes
                    cell.Genome.Genes.Add(New Gene(gene))
                Next

                cell.ATP -= integrationCost
            End If

            Return True
        End Function

        ''' <summary>
        ''' 将外源DNA片段降解为核苷酸
        ''' 
        ''' 每个基因产生9个核苷酸（Gene.LengthInNucleotides = 9）
        ''' 同时产生少量磷酸盐
        ''' </summary>
        Private Sub DegradeDNAToNucleotides(cell As Cell, fragment As Replicon)
            Dim nucleotides = fragment.NucleotideLength
            cell.AddMoleculeInternal(MoleculeType.Nucleotide, nucleotides)
            cell.AddMoleculeInternal(MoleculeType.Phosphate, CInt(Math.Ceiling(nucleotides / 3.0)))
            ' 降解产生少量碳源
            cell.AddMoleculeInternal(MoleculeType.CarbonSource, fragment.Genes.Count)
        End Sub

    End Class

End Namespace
