Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports RNG = Microsoft.VisualBasic.Math.RandomExtensions

Namespace BiologicalRules

    Public Class Regulation

        ReadOnly cell As Cell
        ''' <summary>
        ''' 构建蛋白质丰度加权的功能列表
        ''' </summary>
        ReadOnly candidates As New List(Of WeightedFunction)

        Public ReadOnly Property FunctionRank As WeightedFunction()
            Get
                Return candidates _
                    .OrderByDescending(Function(a) a.weight) _
                    .ToArray
            End Get
        End Property

        Public ReadOnly Property Phenotype As WeightedFunction?
            Get
                If candidates.Count = 0 Then
                    Return Nothing
                End If

                ' 加权随机选择
                Dim totalWeight = candidates.Sum(Function(c) c.weight)
                Dim r = RNG.NextDouble() * totalWeight
                Dim cumulative = 0.0

                For Each c As WeightedFunction In candidates
                    cumulative += c.weight
                    If r <= cumulative Then
                        Return c
                    End If
                Next

                Return candidates.Last
            End Get
        End Property

        Sub New(cell As Cell)
            Me.cell = cell
        End Sub

        Public Function RankProteins(env As NaturalEnvironment) As Regulation
            For Each kvp In cell.Proteins
                If kvp.Value > 0 Then
                    Call RankProtein(kvp.Key, kvp.Value, env)
                End If
            Next

            Return Me
        End Function

        Private Sub RankProtein(protein As GeneOntology, w As Double, env As NaturalEnvironment)
            ' ATP低时，能量代谢优先
            If cell.ATP < 500 Then
                If protein = GeneOntology.AerobicEnergyMetabolismATP OrElse
                   protein = GeneOntology.AnaerobicEnergyMetabolismATP OrElse
                   protein = GeneOntology.GlucoseConversionEnzyme OrElse
                   protein = GeneOntology.PyruvateEnzyme OrElse
                   protein = GeneOntology.AcetateEnzyme OrElse
                   protein = GeneOntology.LactateDehydrogenase OrElse
                   protein = GeneOntology.SuccinateEnzyme OrElse
                   protein = GeneOntology.EthanolMetabolism OrElse
                   protein = GeneOntology.FormateMetabolism OrElse
                   protein = GeneOntology.ButyrateEnzyme Then
                    w *= 3.0
                End If
            End If

            ' 氧气低时，厌氧代谢优先
            If cell.GetMoleculeAmount(MoleculeType.Oxygen) < 10 Then
                If protein = GeneOntology.AnaerobicEnergyMetabolismATP OrElse
                   protein = GeneOntology.LactateDehydrogenase Then
                    w *= 5.0
                End If
            End If

            ' 有抗生素时，降解抗生素优先
            If cell.GetMoleculeAmount(MoleculeType.Antibiotic) > 0 OrElse
               cell.GetMoleculeAmount(MoleculeType.Toxin) > 0 Then
                If protein = GeneOntology.DegradeAntibiotic Then
                    w *= 10.0
                End If
            End If

            ' [v3.0] 高温时，耐热蛋白优先表达
            If cell.InternalTemperature > 40 Then
                If protein = GeneOntology.Thermotolerance Then
                    w *= 8.0
                End If
            End If

            ' [v3.0] 低温时，冷休克响应优先
            If cell.InternalTemperature < 10 Then
                If protein = GeneOntology.ColdShockResponse Then
                    w *= 8.0
                End If
            End If

            ' [v3.0] 渗透压失衡时，渗透调节优先
            Dim voxel = env(cell.Position.X, cell.Position.Y, cell.Position.Z)
            Dim osmDiff = Math.Abs(voxel.ExternalIonStrength - cell.InternalIonStrength)
            If osmDiff > 50 Then
                If protein = GeneOntology.Osmoregulation OrElse
                   protein = GeneOntology.CompatibleSoluteSynthesis Then
                    w *= 5.0
                End If
            End If

            candidates.Add(New WeightedFunction With {.func = protein, .weight = w})
        End Sub

    End Class

    Public Structure WeightedFunction

        Dim func As GeneOntology
        Dim weight As Double

        Public Overrides Function ToString() As String
            Return $"({func.Description}, weight={weight:F3})"
        End Function

    End Structure
End Namespace