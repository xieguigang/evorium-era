Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports rng = Microsoft.VisualBasic.Math.RandomExtensions

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v4.0 新增] 环境DNA降解规则
    ''' 
    ''' 规则逻辑：
    ''' 环境中的降解酶（具有NucleicAcidDegradation或DegradeMacromolecule功能的
    ''' 活性蛋白质）可以在环境中将DNA片段降解为核苷酸。
    ''' 
    ''' 本规则为环境级别规则（IEnvironmentRule），在每个迭代的环境阶段执行。
    ''' 与ExtracellularProteinActivityRule类似，处理环境中大分子的降解。
    ''' 
    ''' 生物学意义：
    ''' - 环境中的核酸酶（DNase）可以降解游离DNA
    ''' - 降解产物（核苷酸、磷酸盐）可被其他细胞利用
    ''' - 这是环境中DNA周转的关键机制
    ''' </summary>
    Public Class EnvironmentalDNADegradationRule : Implements IEnvironmentRule

        ''' <summary>
        ''' 环境级别执行：遍历所有格子，处理环境DNA的酶促降解
        ''' </summary>
        Public Sub ExecuteEnvironment(env As NaturalEnvironment, iteration As Long) Implements IEnvironmentRule.ExecuteEnvironment
            For Each voxel In env.AllVoxels()
                ' 检查环境中是否有DNA
                Dim dnaMol = voxel.ExternalMolecules.TryGetValue(MoleculeType.DNA)
                If dnaMol Is Nothing Then Continue For

                Dim envDNA As DNAMolecule = dnaMol
                If envDNA.DNAFragments.Count = 0 Then Continue For

                ' 检查环境中是否有活性的核酸降解酶
                Dim proteinMol = voxel.ExternalMolecules.TryGetValue(MoleculeType.Protein)
                If proteinMol Is Nothing Then Continue For

                Dim extProteins As ProteinMolecule = proteinMol
                Dim activeDNADegraders = 0

                If extProteins.Proteins.ContainsKey(GeneOntology.NucleicAcidDegradation) Then
                    For Each prot In extProteins.Proteins(GeneOntology.NucleicAcidDegradation)
                        If prot.ViabilityDuration > 0 Then
                            activeDNADegraders += 1
                        End If
                    Next
                End If

                ' DegradeMacromolecule也可以降解DNA（非特异性降解，效率较低）
                If extProteins.Proteins.ContainsKey(GeneOntology.DegradeMacromolecule) Then
                    For Each prot In extProteins.Proteins(GeneOntology.DegradeMacromolecule)
                        If prot.ViabilityDuration > 0 Then
                            activeDNADegraders += 1
                        End If
                    Next
                End If

                If activeDNADegraders = 0 Then Continue For

                ' 每个活性降解酶每周期最多降解1个DNA片段
                Dim maxDegrade = activeDNADegraders
                Dim degraded = 0

                While degraded < maxDegrade AndAlso envDNA.DNAFragments.Count > 0
                    ' 随机选择一个DNA片段降解
                    Dim index = rng.Next(envDNA.DNAFragments.Count)
                    Dim fragment = envDNA.DNAFragments(index)
                    envDNA.DNAFragments.RemoveAt(index)

                    ' 降解产生核苷酸和磷酸盐释放到环境中
                    Dim nucleotides = fragment.NucleotideLength
                    env.AddMolecule(voxel, MoleculeType.Nucleotide, nucleotides)
                    env.AddMolecule(voxel, MoleculeType.Phosphate, CInt(Math.Ceiling(nucleotides / 3.0)))
                    env.AddMolecule(voxel, MoleculeType.CarbonSource, fragment.Genes.Count)

                    degraded += 1
                End While
            Next
        End Sub

    End Class

End Namespace
