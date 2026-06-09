Imports System.Runtime.CompilerServices

Namespace Models.Container

    ''' <summary>
    ''' pH近似估算工具
    ''' 
    ''' 基于IVoxel中的分子组成，利用酸碱平衡原理近似估算环境的pH值。
    ''' 
    ''' ===== 计算原理 =====
    ''' 
    ''' 1. 强酸强碱（H+、OH-）直接中和
    ''' 2. CO2溶于水形成碳酸（H2CO3），贡献H+（pKa1=6.35, pKa2=10.33）
    ''' 3. 碳酸根（CO3²-）为碱，消耗H+
    ''' 4. 有机酸阴离子（乳酸根、乙酸根等）为共轭碱，少量消耗H+
    ''' 5. 硫化物、磷酸盐为弱碱
    ''' 6. 氨基酸两性，中性pH下微弱碱性
    ''' 7. 考虑水的自离子化平衡：Kw = [H+][OH-]
    ''' 8. 支持温度修正Kw值（Harned &amp; Hamer, 1933经验公式）
    ''' 
    ''' ===== 浓度换算 =====
    ''' 
    ''' 模拟中的分子数量（整数）通过浓度换算因子c0映射为摩尔浓度。
    ''' 默认c0=1e-6，即1个模拟单位≈1μM。
    ''' 在此映射下：
    '''   - 20单位H+ → pH≈4.7（触发酸代谢的阈值）
    '''   -  5单位H+ → pH≈5.3
    '''   -  0单位H+ → pH=7.0（纯水中性）
    '''   - 20单位OH- → pH≈9.3
    ''' 
    ''' ===== 使用示例 =====
    ''' 
    '''   ' 环境格子，使用格子温度
    '''   Dim pH = pHEstimator.EstimatepH(voxel, temperatureC:=voxel.Temperature)
    ''' 
    '''   ' 细胞内pH，使用细胞内温度
    '''   Dim pH = pHEstimator.EstimatepH(cell, temperatureC:=cell.InternalTemperature)
    ''' 
    '''   ' 默认25°C
    '''   Dim pH = pHEstimator.EstimatepH(voxel)
    ''' </summary>
    Public Module PHHelper

        ''' <summary>
        ''' 默认浓度换算因子：1个模拟单位 ≈ 1μM
        ''' </summary>
        Public Const DefaultConcentrationScale As Double = 0.000001

        ''' <summary>
        ''' 估算 IVoxel 所表示微环境的近似 pH 值。
        ''' 假设每个 Voxel 为单位体积，且分子数量直接代表浓度（无单位）。
        ''' </summary>
        Public Function EstimatePH(voxel As IVoxel) As Double
            If voxel?.Molecules Is Nothing Then
                ' 无分子数据，默认中性
                Return 7.0
            Else
                ' 遍历分子字典，获取氢离子和氢氧根离子数量
                Dim hCount As Integer = voxel.Molecules.TryGetValue(MoleculeType.HydrogenIon)?.Quantity
                Dim ohCount As Integer = voxel.Molecules.TryGetValue(MoleculeType.HydroxideIon)?.Quantity

                Return EstimatePH(hCount, ohCount)
            End If
        End Function

        ''' <summary>
        ''' 避免对数计算出现零或负值
        ''' </summary>
        Public Const epsilon As Double = 0.000000001

        Public Function EstimatePH(H As Integer, OH As Integer) As Double
            ' 处理极端情况：无离子时返回中性
            If H = 0 AndAlso OH = 0 Then
                Return 7.0
            Else
                If H >= OH Then
                    ' 酸性或中性：pH = -log10([H+])
                    Dim netH As Double = CDbl(H - OH)
                    If netH <= 0 Then netH = epsilon
                    Return -Math.Log10(netH)
                Else
                    ' 碱性：pOH = -log10([OH-])，pH = 14 - pOH
                    Dim netOH As Double = CDbl(OH - H)
                    If netOH <= 0 Then netOH = epsilon
                    Dim pOH As Double = -Math.Log10(netOH)
                    Return 14.0 - pOH
                End If
            End If
        End Function

        ''' <summary>
        ''' 近似估算IVoxel的pH值
        ''' </summary>
        ''' <param name="voxel">要估算pH的IVoxel对象（Cell或Voxel）</param>
        ''' <param name="c0">浓度换算因子，1个分子单位对应的摩尔浓度（M）。
        '''   默认1e-6，即1单位≈1μM。可调整以匹配不同模拟尺度。</param>
        ''' <param name="temperatureC">温度（°C），用于修正Kw。默认25°C。
        '''   建议对Voxel传入voxel.Temperature，对Cell传入cell.InternalTemperature。</param>
        ''' <returns>pH值，范围0.0-14.0</returns>
        ''' 
        <Extension>
        Public Function EstimatePH(voxel As IVoxel,
                                   Optional c0 As Double = DefaultConcentrationScale,
                                   Optional temperatureC As Double = 25.0) As Double

            ' ===== 温度修正Kw =====
            ' Harned & Hamer (1933) 经验公式：
            '   pKw = 14.94 - 0.0420*T + 0.000168*T²
            ' 验证：25°C→pKw≈14.00, 0°C→pKw≈14.94, 50°C→pKw≈13.26
            Dim pKw As Double = 14.94 - 0.042 * temperatureC + 0.000168 * temperatureC * temperatureC
            Dim Kw As Double = Math.Pow(10, -pKw)

            ' ===== 获取各酸碱相关分子的数量 =====
            Dim H = CDbl(GetMoleculeCount(voxel, MoleculeType.HydrogenIon))
            Dim OH = CDbl(GetMoleculeCount(voxel, MoleculeType.HydroxideIon))

            ' ===== 强酸强碱中和 =====
            Dim netH As Double = H - OH

            ' ================================================================
            '  弱酸贡献（增加有效H+）
            ' ================================================================

            ' CO2溶于水形成碳酸：CO2 + H2O ⇌ H2CO3 ⇌ H+ + HCO3-
            ' pKa1=6.35, pKa2=10.33
            ' 在pH≈7时，约82%以HCO3-形式存在，每个HCO3-的形成释放1个H+
            ' 有效产酸贡献约为CO2浓度的35%（考虑缓冲和二次解离的折减）
            netH += CDbl(GetMoleculeCount(voxel, MoleculeType.CarbonDioxide)) * 0.35

            ' 脂肪酸（pKa≈4.8）：弱酸
            ' 在pH 7时大部分已解离为阴离子，保留少量未解离酸的H+贡献
            netH += CDbl(GetMoleculeCount(voxel, MoleculeType.FattyAcid)) * 0.1

            ' ================================================================
            '  弱碱贡献（消耗有效H+）
            ' ================================================================

            ' 碳酸根 CO3²- + H+ → HCO3-（pKb1=3.67，强碱）
            ' 在pH 7时几乎完全质子化为HCO3-，每个CO3²-消耗约1个H+
            ' 系数0.8考虑二次质子化不完全
            netH -= CDbl(GetMoleculeCount(voxel, MoleculeType.CarbonateIon)) * 0.8

            ' 有机酸阴离子（共轭碱），在pH 7时少量质子化
            ' 乳酸根 (pKa=3.86): 质子化分数≈0.07%
            ' 乙酸根 (pKa=4.76): 质子化分数≈0.6%
            ' 甲酸根 (pKa=3.75): 质子化分数≈0.06%
            ' 丁酸根 (pKa=4.82): 质子化分数≈0.6%
            ' 琥珀酸根 (pKa1=4.21, pKa2=5.64): 二元酸根，可消耗2个H+
            netH -= CDbl(GetMoleculeCount(voxel, MoleculeType.Lactate)) * 0.01
            netH -= CDbl(GetMoleculeCount(voxel, MoleculeType.Acetate)) * 0.005
            netH -= CDbl(GetMoleculeCount(voxel, MoleculeType.Formate)) * 0.01
            netH -= CDbl(GetMoleculeCount(voxel, MoleculeType.Butyrate)) * 0.005
            netH -= CDbl(GetMoleculeCount(voxel, MoleculeType.Succinate)) * 0.02

            ' 硫化物 S²- (pKb1≈7.0, pKb2≈12.9)
            ' 在pH 7时约50%质子化为HS-
            netH -= CDbl(GetMoleculeCount(voxel, MoleculeType.Sulfide)) * 0.5

            ' 磷酸盐 PO4³- (pKa1=2.1, pKa2=7.2, pKa3=12.3)
            ' 在pH 7时主要以H2PO4-和HPO4²-形式存在，作为缓冲体系
            ' 净效果微弱碱性（HPO4²-可再接受1个H+）
            netH -= CDbl(GetMoleculeCount(voxel, MoleculeType.Phosphate)) * 0.05

            ' 氨基酸两性分子，中性pH下等电点附近微弱碱性
            ' （氨基pKa≈9-10，羧基pKa≈2-3，等电点5-6，pH 7时净带负电）
            netH -= CDbl(GetMoleculeCount(voxel, MoleculeType.AminoMixGluFamily)) * 0.005
            netH -= CDbl(GetMoleculeCount(voxel, MoleculeType.AminoMixAspFamily)) * 0.005
            netH -= CDbl(GetMoleculeCount(voxel, MoleculeType.AminoMixSerGly)) * 0.005
            netH -= CDbl(GetMoleculeCount(voxel, MoleculeType.AminoMixAromatic)) * 0.003
            netH -= CDbl(GetMoleculeCount(voxel, MoleculeType.AminoMixBranched)) * 0.003
            netH -= CDbl(GetMoleculeCount(voxel, MoleculeType.AminoMixThiol)) * 0.003

            ' ================================================================
            '  求解pH
            ' ================================================================
            ' 考虑水的自离子化平衡：Kw = [H+][OH-]
            '
            ' 酸性情况（netH > 0）：
            '   设C = netH × c0 为净酸浓度
            '   电荷守恒：[H+] = C + [OH-] = C + Kw/[H+]
            '   整理得：[H+]² - C·[H+] - Kw = 0
            '   求解：[H+] = (C + √(C² + 4Kw)) / 2
            '
            ' 碱性情况（netH < 0）：
            '   设C = |netH| × c0 为净碱浓度
            '   电荷守恒：[OH-] = C + [H+] = C + Kw/[OH-]
            '   整理得：[OH-]² - C·[OH-] - Kw = 0
            '   求解：[OH-] = (C + √(C² + 4Kw)) / 2
            '
            ' 中性情况（netH = 0）：
            '   [H+] = [OH-] = √Kw → pH = pKw/2

            Dim pH As Double

            If netH > 0 Then
                ' 酸性
                Dim C = netH * c0
                Dim H_conc = (C + Math.Sqrt(C * C + 4.0 * Kw)) / 2.0
                pH = -Math.Log10(H_conc)
            ElseIf netH < 0 Then
                ' 碱性
                Dim C = Math.Abs(netH) * c0
                Dim OH_conc = (C + Math.Sqrt(C * C + 4.0 * Kw)) / 2.0
                Dim pOH = -Math.Log10(OH_conc)
                pH = pKw - pOH
            Else
                ' 中性（纯水自离子化）
                pH = pKw / 2.0
            End If

            Return Math.Max(0.0, Math.Min(14.0, pH))
        End Function

        ''' <summary>
        ''' 从IVoxel的Molecules字典中获取指定类型分子的数量
        ''' </summary>
        Private Function GetMoleculeCount(voxel As IVoxel, type As MoleculeType) As Integer
            Dim mol As Molecule = Nothing
            If voxel.Molecules.TryGetValue(type, mol) Then
                Return mol.Quantity
            End If
            Return 0
        End Function
    End Module
End Namespace