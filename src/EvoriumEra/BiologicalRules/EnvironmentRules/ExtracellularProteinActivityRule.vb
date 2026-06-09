Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports rng = Microsoft.VisualBasic.Math.RandomExtensions

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v4.0 新增] 胞外蛋白质活性规则
    ''' 
    ''' 规则逻辑：
    ''' 1. 细胞裂解释放到环境中或细胞主动分泌到胞外的蛋白质，在5个循环周期内
    '''    仍然保持生物学活性，可以利用环境中的ATP分子来执行相应的生物学功能
    ''' 2. 当5个循环周期之后，蛋白质将会失活，等待被降解酶降解成为氨基酸
    ''' 3. 环境中具有DegradeMacromolecule或ProteinDegradation功能的活性蛋白质
    '''    可以降解环境中已失活的蛋白质，将其转化为氨基酸释放到环境中
    ''' 
    ''' 本规则为环境级别规则（IEnvironmentRule），在每个迭代的环境阶段执行。
    ''' 
    ''' 生物学意义：
    ''' - 胞外酶（如消化酶、降解酶）在释放到环境后仍可短期发挥催化功能
    ''' - 这模拟了自然界中胞外酶的"残余活性"现象
    ''' - 失活后的蛋白质成为氨基酸的来源，构成营养循环的一部分
    ''' </summary>
    Public Class ExtracellularProteinActivityRule : Implements IEnvironmentRule

        ''' <summary>
        ''' 环境级别执行：遍历所有格子，处理胞外蛋白质的活性衰减、功能执行和降解
        ''' </summary>
        Public Sub ExecuteEnvironment(env As NaturalEnvironment, iteration As Long) Implements IEnvironmentRule.ExecuteEnvironment
            Dim config = env.configs

            For Each voxel In env.AllVoxels()
                ' 获取该格子中的蛋白质分子
                Dim proteinMol = voxel.ExternalMolecules.TryGetValue(MoleculeType.Protein)
                If proteinMol Is Nothing Then Continue For

                Dim extProteins As ProteinMolecule = proteinMol
                If extProteins.Proteins.Count = 0 Then Continue For

                ' ===== 阶段1：活性衰减 =====
                ' 每个循环周期，所有胞外蛋白质的ViabilityDuration减1
                For Each kvp In extProteins.Proteins
                    For Each prot In kvp.Value
                        prot.ViabilityDuration -= 1
                    Next
                Next

                ' ===== 阶段2：活性蛋白质执行功能 =====
                ' 活性蛋白质（ViabilityDuration > 0）可以消耗环境ATP执行功能
                ExecuteActiveProteinFunctions(voxel, extProteins, env, config)

                ' ===== 阶段3：失活蛋白质降解 =====
                ' 环境中的降解酶（DegradeMacromolecule/ProteinDegradation）可以降解失活蛋白质
                DegradeInactiveProteins(voxel, extProteins, env, config)

                ' ===== 阶段4：清理空条目 =====
                CleanupEmptyProteinEntries(extProteins)
            Next
        End Sub

        ''' <summary>
        ''' 阶段2：活性蛋白质消耗环境ATP执行生物学功能
        ''' 
        ''' 每个活性蛋白质每周期尝试执行一次功能：
        ''' - 消耗1个环境ATP
        ''' - 根据蛋白质功能类型执行相应的催化反应
        ''' - 反应产物释放到环境中
        ''' 
        ''' 注意：只有当环境中有足够ATP时，蛋白质才能执行功能
        ''' </summary>
        Private Sub ExecuteActiveProteinFunctions(voxel As Voxel, extProteins As ProteinMolecule, env As NaturalEnvironment, config As Configs)
            ' 获取环境中的ATP数量
            Dim envATP = voxel.GetMoleculeAmount(MoleculeType.ATP)
            If envATP <= 0 Then Return

            Dim atpCost = config.ExtracellularProteinATPCost
            Dim activityProb = config.ExtracellularProteinActivityProb

            For Each kvp In extProteins.Proteins.ToList()
                Dim proteinFunc = kvp.Key
                Dim activeProteins = kvp.Value.Where(Function(p) p.ViabilityDuration > 0).ToList()

                For Each prot In activeProteins
                    ' 检查环境ATP是否足够
                    If envATP < atpCost Then Exit For

                    ' 概率性执行：不是每个活性蛋白质每周期都执行
                    If rng.NextDouble() > activityProb Then Continue For

                    ' 消耗环境ATP
                    env.AddMolecule(voxel, MoleculeType.ATP, -atpCost)
                    envATP -= atpCost

                    ' 根据蛋白质功能类型执行催化反应
                    ExecuteProteinFunction(proteinFunc, voxel, env)
                Next
            Next
        End Sub

        ''' <summary>
        ''' 根据蛋白质功能类型执行相应的催化反应
        ''' 
        ''' 胞外蛋白质主要执行以下类型的催化功能：
        ''' - 降解酶：降解环境中的大分子（蛋白质、DNA等）
        ''' - 消化酶：将复杂底物分解为简单分子
        ''' - 代谢酶：催化环境中的代谢转化
        ''' </summary>
        Private Sub ExecuteProteinFunction(proteinFunc As GeneOntology, voxel As Voxel, env As NaturalEnvironment)
            Select Case proteinFunc
                ' ===== 降解酶类 =====
                Case GeneOntology.DegradeMacromolecule
                    ' 非特异性大分子降解：将环境中的复杂分子降解为简单分子
                    ' 降解碳源 → 产生葡萄糖
                    If voxel.GetMoleculeAmount(MoleculeType.CarbonSource) > 0 Then
                        env.AddMolecule(voxel, MoleculeType.CarbonSource, -1)
                        env.AddMolecule(voxel, MoleculeType.Glucose, 1)
                    End If

                Case GeneOntology.ProteinDegradation
                    ' 蛋白质降解酶：降解环境中已失活的蛋白质
                    ' 此功能在阶段3中专门处理，这里不再重复

                Case GeneOntology.NucleicAcidDegradation
                    ' 核酸降解酶：降解环境中的DNA片段
                    ' 此功能由EnvironmentalDNADegradationRule处理

                ' ===== 消化酶类 =====
                Case GeneOntology.GlucoseConversionEnzyme
                    ' 胞外葡萄糖转化酶：将环境中的葡萄糖转化为丙酮酸
                    If voxel.GetMoleculeAmount(MoleculeType.Glucose) > 0 Then
                        env.AddMolecule(voxel, MoleculeType.Glucose, -1)
                        env.AddMolecule(voxel, MoleculeType.Pyruvate, 2)
                    End If

                Case GeneOntology.PyruvateEnzyme
                    ' 胞外丙酮酸代谢酶：将环境中的丙酮酸转化为醋酸盐
                    If voxel.GetMoleculeAmount(MoleculeType.Pyruvate) > 0 Then
                        env.AddMolecule(voxel, MoleculeType.Pyruvate, -1)
                        env.AddMolecule(voxel, MoleculeType.Acetate, 1)
                        env.AddMolecule(voxel, MoleculeType.CarbonDioxide, 1)
                    End If

                Case GeneOntology.AcetateEnzyme
                    ' 胞外醋酸盐代谢酶：将环境中的醋酸盐进一步代谢
                    If voxel.GetMoleculeAmount(MoleculeType.Acetate) > 0 Then
                        env.AddMolecule(voxel, MoleculeType.Acetate, -1)
                        env.AddMolecule(voxel, MoleculeType.CarbonDioxide, 2)
                    End If

                ' ===== 其他胞外酶 =====
                Case GeneOntology.LactateDehydrogenase
                    ' 胞外乳酸脱氢酶：将环境中的乳酸转化为丙酮酸
                    If voxel.GetMoleculeAmount(MoleculeType.Lactate) > 0 Then
                        env.AddMolecule(voxel, MoleculeType.Lactate, -1)
                        env.AddMolecule(voxel, MoleculeType.Pyruvate, 1)
                    End If

                Case Else
                    ' 其他类型的胞外蛋白质暂不执行特定功能
                    ' 但仍然消耗ATP（已在上层方法中扣除）
            End Select
        End Sub

        ''' <summary>
        ''' 阶段3：降解环境中已失活的蛋白质
        ''' 
        ''' 环境中具有DegradeMacromolecule或ProteinDegradation功能的活性蛋白质
        ''' 可以降解环境中已失活的蛋白质（ViabilityDuration <= 0）。
        ''' 每个活性降解酶每周期最多降解1个失活蛋白质。
        ''' 降解产物为氨基酸，释放到环境中。
        ''' </summary>
        Private Sub DegradeInactiveProteins(voxel As Voxel, extProteins As ProteinMolecule, env As NaturalEnvironment, config As Configs)
            ' 统计活性降解酶数量
            Dim activeDegraders = 0

            If extProteins.Proteins.ContainsKey(GeneOntology.DegradeMacromolecule) Then
                For Each prot In extProteins.Proteins(GeneOntology.DegradeMacromolecule)
                    If prot.ViabilityDuration > 0 Then activeDegraders += 1
                Next
            End If

            If extProteins.Proteins.ContainsKey(GeneOntology.ProteinDegradation) Then
                For Each prot In extProteins.Proteins(GeneOntology.ProteinDegradation)
                    If prot.ViabilityDuration > 0 Then activeDegraders += 1
                Next
            End If

            If activeDegraders = 0 Then Return

            ' 收集所有失活的蛋白质
            Dim inactiveProteins As New List(Of ExtracellularProtein)
            For Each kvp In extProteins.Proteins
                For Each prot In kvp.Value
                    If prot.ViabilityDuration <= 0 Then
                        inactiveProteins.Add(prot)
                    End If
                Next
            Next

            If inactiveProteins.Count = 0 Then Return

            ' 每个活性降解酶每周期最多降解1个失活蛋白质
            Dim maxDegrade = Math.Min(activeDegraders, inactiveProteins.Count)
            Dim degraded = 0

            ' 随机打乱失活蛋白质列表，随机选择降解目标
            Dim shuffled = inactiveProteins.OrderBy(Function(x) rng.NextDouble()).ToList()

            For Each prot In shuffled
                If degraded >= maxDegrade Then Exit For

                ' 从ProteinMolecule中移除该失活蛋白质
                extProteins.Proteins(prot.Term).Remove(prot)

                ' 降解产生氨基酸：每个蛋白质产生3个氨基酸（混合类型）
                env.AddMolecule(voxel, MoleculeType.AminoMixGluFamily, 1)
                env.AddMolecule(voxel, MoleculeType.AminoMixAspFamily, 1)
                env.AddMolecule(voxel, MoleculeType.AminoMixSerGly, 1)

                ' 同时产生少量碳源和氮源
                env.AddMolecule(voxel, MoleculeType.CarbonSource, 1)
                env.AddMolecule(voxel, MoleculeType.NitrogenSource, 1)

                degraded += 1
            Next
        End Sub

        ''' <summary>
        ''' 清理ProteinMolecule中空的蛋白质条目
        ''' </summary>
        Private Sub CleanupEmptyProteinEntries(proteinMol As ProteinMolecule)
            Dim emptyKeys = proteinMol.Proteins.Where(Function(kvp) kvp.Value.Count = 0).
                                               Select(Function(kvp) kvp.Key).ToList()

            For Each key In emptyKeys
                proteinMol.Proteins.Remove(key)
            Next
        End Sub

    End Class

End Namespace
