Namespace Models.Container

    Public Module PHHelper

        ''' <summary>
        ''' 估算 IVoxel 所表示微环境的近似 pH 值。
        ''' 假设每个 Voxel 为单位体积，且分子数量直接代表浓度（无单位）。
        ''' </summary>
        Public Function EstimatePH(voxel As IVoxel) As Double
            If voxel?.Molecules Is Nothing Then Return 7.0 ' 无分子数据，默认中性

            Dim hCount As Integer = 0
            Dim ohCount As Integer = 0

            ' 遍历分子字典，获取氢离子和氢氧根离子数量
            For Each kvp In voxel.Molecules
                Select Case kvp.Key
                    Case MoleculeType.HydrogenIon
                        hCount = kvp.Value.Quantity
                    Case MoleculeType.HydroxideIon
                        ohCount = kvp.Value.Quantity
                End Select
            Next

            ' 处理极端情况：无离子时返回中性
            If hCount = 0 AndAlso ohCount = 0 Then Return 7.0

            ' 避免对数计算出现零或负值
            Const epsilon As Double = 0.000000001

            If hCount >= ohCount Then
                ' 酸性或中性：pH = -log10([H+])
                Dim netH As Double = CDbl(hCount - ohCount)
                If netH <= 0 Then netH = epsilon
                Return -Math.Log10(netH)
            Else
                ' 碱性：pOH = -log10([OH-])，pH = 14 - pOH
                Dim netOH As Double = CDbl(ohCount - hCount)
                If netOH <= 0 Then netOH = epsilon
                Dim pOH As Double = -Math.Log10(netOH)
                Return 14.0 - pOH
            End If
        End Function

    End Module
End Namespace