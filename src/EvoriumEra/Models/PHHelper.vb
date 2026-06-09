Namespace Models.Container

    Public Module PHHelper

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
        Const epsilon As Double = 0.000000001

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
    End Module
End Namespace