Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v3.0 核心] 温度规则
    ''' 
    ''' 温度系统包含以下机制：
    ''' 
    ''' 1. 昼夜温度循环：
    '''    T(t) = BaseTemp + Amplitude * sin(2π * t / Period)
    '''    模拟自然环境日间升温、夜间降温
    ''' 
    ''' 2. 深度温度梯度：
    '''    T(z) = T_surface - z * DepthDecay
    '''    表层温暖，深层凉爽
    ''' 
    ''' 3. 代谢产热：
    '''    细胞消耗ATP时，所在格子温度微升
    '''    在ConsumeBasicResources中实现
    ''' 
    ''' 4. 热扩散：
    '''    相邻格子间温度趋向均衡
    '''    扩散速率由HeatDiffusionRate控制
    ''' 
    ''' 5. 蛋白质温度响应：
    '''    - 高温失活（不可逆）：T > DenaturationTemp时，非耐热蛋白逐步失活
    '''    - 低温活性降低（可逆）：T &lt; ColdShockTemp时，蛋白活性下降
    '''    - 耐热蛋白(Thermotolerance)保护其他蛋白
    '''    - 冷休克响应(ColdShockResponse)缓解低温影响
    ''' 
    ''' 6. 温度恢复：
    '''    格子温度向环境基线缓慢恢复
    ''' </summary>
    Public Class TemperatureRule : Inherits IBiochemicalRule
        Implements IEnvironmentRule

        Sub New()
            Call MyBase.New() ' 全局规则
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
            Dim currentTemp = voxel.Temperature

            ' ===== 高温蛋白失活 =====
            If currentTemp > 45.0 Then ' ProteinDenaturationTemp
                Dim denaturationRate = CalculateDenaturationRate(currentTemp, cell)

                If denaturationRate > 0 Then
                    ' 随机选择非耐热蛋白进行失活
                    Dim proteinsToDenature = New List(Of GeneOntology)
                    For Each kvp In cell.Proteins.ToList()
                        ' 耐热蛋白自身不会被失活
                        If kvp.Key <> GeneOntology.Thermotolerance AndAlso kvp.Value > 0 Then
                            proteinsToDenature.Add(kvp.Key)
                        End If
                    Next

                    If proteinsToDenature.Any() Then
                        ' 按失活率随机失活蛋白
                        Dim numToDenature = Math.Max(1, CInt(proteinsToDenature.Count * denaturationRate))
                        For i = 0 To Math.Min(numToDenature - 1, proteinsToDenature.Count - 1)
                            Dim targetProtein = proteinsToDenature(i)
                            If cell.Proteins.ContainsKey(targetProtein) Then
                                cell.Proteins(targetProtein) -= 1
                                If cell.Proteins(targetProtein) <= 0 Then
                                    cell.Proteins.Remove(targetProtein)
                                End If
                            End If
                        Next
                    End If
                End If
            End If

            ' ===== 低温活性修正 =====
            ' 在Cell.ProteinActivityFactor中自动计算
            ' 这里处理冷休克响应蛋白的效果
            If currentTemp < 15.0 Then ' ColdShockTemp
                ' 有冷休克响应蛋白的细胞，低温活性惩罚减半
                If cell.HasFunction(GeneOntology.ColdShockResponse) Then
                    cell.ColdShockMitigation = 0.5 ' 活性惩罚减半
                Else
                    cell.ColdShockMitigation = 0.0
                End If
            Else
                cell.ColdShockMitigation = 0.0
            End If

            ' ===== 细胞内温度同步到格子 =====
            ' 代谢产热已在ConsumeBasicResources中累积到cell.InternalTemperature
            If cell.InternalTemperature > 0 Then
                voxel.Temperature += cell.InternalTemperature
                cell.InternalTemperature = 0
            End If
        End Sub

        ''' <summary>
        ''' 环境级别的温度计算
        ''' </summary>
        Public Sub ExecuteEnvironment(env As NaturalEnvironment, iteration As Long) Implements IEnvironmentRule.ExecuteEnvironment
            Dim config = env.configs
            ' 1. 计算当前昼夜温度偏移
            Dim diurnalOffset = config.DiurnalTemperatureAmplitude *
                                Math.Sin(2.0 * Math.PI * iteration / config.DiurnalPeriod)
            Dim dims = env.Dimensions

            ' 2. 更新每个格子的环境基线温度
            For x As Integer = 0 To dims.Width - 1
                For y As Integer = 0 To dims.Height - 1
                    For z As Integer = 0 To dims.Depth - 1
                        Dim voxel = env.Grid(x, y, z)

                        ' 环境基线 = 基础温度 + 昼夜偏移 - 深度衰减
                        Dim baselineTemp = config.BaseTemperature + diurnalOffset - z * config.TemperatureDepthDecay

                        ' 温度向基线恢复
                        voxel.Temperature += (baselineTemp - voxel.Temperature) * config.TemperatureRecoveryRate
                    Next
                Next
            Next

            ' 3. 热扩散
            ExecuteHeatDiffusion(env)
        End Sub

        Private Sub ExecuteHeatDiffusion(env As NaturalEnvironment)
            ' 收集温度变化量，避免就地修改影响计算
            Dim tempChanges = New Dictionary(Of (Integer, Integer, Integer), Double)
            Dim dims = env.Dimensions
            Dim config = env.configs

            For x As Integer = 0 To dims.Width - 1
                For y As Integer = 0 To dims.Height - 1
                    For z As Integer = 0 To dims.Depth - 1
                        Dim voxel = env.Grid(x, y, z)
                        Dim neighbors = env.GetNeighbors(voxel)

                        Dim totalDiff = 0.0
                        For Each neighbor In neighbors
                            Dim diff = neighbor.Temperature - voxel.Temperature
                            totalDiff += diff * config.HeatDiffusionRate
                        Next

                        tempChanges((x, y, z)) = totalDiff
                    Next
                Next
            Next

            ' 应用温度变化
            For Each kvp In tempChanges
                Dim voxel = env.Grid(kvp.Key.Item1, kvp.Key.Item2, kvp.Key.Item3)
                voxel.Temperature += kvp.Value
            Next
        End Sub

        ''' <summary>
        ''' 计算蛋白失活率（0.0-1.0）
        ''' </summary>
        Private Function CalculateDenaturationRate(temperature As Double, cell As Cell) As Double
            Dim denaturationTemp = 45.0
            Dim completeDenaturationTemp = 60.0

            If temperature < denaturationTemp Then Return 0.0

            ' 线性插值失活率
            Dim rate = (temperature - denaturationTemp) / (completeDenaturationTemp - denaturationTemp)
            rate = Math.Min(1.0, rate)

            ' 耐热蛋白保护：每个耐热蛋白降低10%失活率，最多降低80%
            If cell.HasFunction(GeneOntology.Thermotolerance) Then
                Dim thermotoleranceCount = cell.Proteins(GeneOntology.Thermotolerance)
                Dim protection = Math.Min(0.8, thermotoleranceCount * 0.1)
                rate *= (1.0 - protection)
            End If

            Return rate
        End Function
    End Class
End Namespace
