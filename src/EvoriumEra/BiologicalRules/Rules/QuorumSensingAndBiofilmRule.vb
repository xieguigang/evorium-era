Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v2.0 改进] 群体感应与生物膜规则
    ''' </summary>
    Public Class QuorumSensingAndBiofilmRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(
                GeneOntology.QuorumSensing,
                GeneOntology.SignalMoleculeSynthesis,
                GeneOntology.SecondaryMetaboliteSynthesis,
                GeneOntology.BiofilmSynthesis
            )
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            ' 信号分子合成
            If cell.HasFunction(GeneOntology.SignalMoleculeSynthesis) Then
                Dim carbon = cell.GetMoleculeAmount(MoleculeType.CarbonSource)
                Dim serGly = cell.GetMoleculeAmount(MoleculeType.AminoMixSerGly)
                If carbon > 0 AndAlso serGly > 0 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, -1)
                        cell.AddMoleculeInternal(MoleculeType.AminoMixSerGly, -1)
                        cell.AddMoleculeInternal(MoleculeType.SignalMolecule, 3)
                    End If
                End If
            End If

            ' 群体感应检测
            Dim quorumActive = False
            If cell.HasFunction(GeneOntology.QuorumSensing) Then
                ' 检测环境中的信号分子
                Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
                Dim envSignal = voxel.GetMoleculeAmount(MoleculeType.SignalMolecule)
                Dim intSignal = cell.GetMoleculeAmount(MoleculeType.SignalMolecule)
                Dim totalSignal = envSignal + intSignal

                quorumActive = QuorumProbability(cell, totalSignal) > 0.5

                ' 群体感应激活时，吸收环境信号分子
                If quorumActive AndAlso envSignal > 0 Then
                    Dim absorb = Math.Min(envSignal, 5)
                    voxel.ExternalMolecules(MoleculeType.SignalMolecule).Quantity -= absorb
                    cell.AddMoleculeInternal(MoleculeType.SignalMolecule, absorb)
                End If
            End If

            ' 次级天然产物合成
            If cell.HasFunction(GeneOntology.SecondaryMetaboliteSynthesis) Then
                Dim acetate = cell.GetMoleculeAmount(MoleculeType.Acetate)
                Dim gluAmino = cell.GetMoleculeAmount(MoleculeType.AminoMixGluFamily)

                ' ATP > 80%时概率升高，或群体感应激活时必定执行
                Dim shouldSynthesize = (cell.ATP > 800 AndAlso QuorumProbability(cell) > 0.3) OrElse quorumActive

                If shouldSynthesize AndAlso acetate >= 2 AndAlso gluAmino >= 1 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.Acetate, -2)
                        cell.AddMoleculeInternal(MoleculeType.AminoMixGluFamily, -1)
                        cell.AddMoleculeInternal(MoleculeType.SecondaryMetabolite, 1)
                    End If
                End If
            End If

            ' 生物膜合成
            If cell.HasFunction(GeneOntology.BiofilmSynthesis) Then
                Dim nitrogen = cell.GetMoleculeAmount(MoleculeType.NitrogenSource)
                Dim aspAmino = cell.GetMoleculeAmount(MoleculeType.AminoMixAspFamily)

                Dim shouldSynthesize = quorumActive OrElse (cell.ATP > 800)

                If shouldSynthesize AndAlso nitrogen >= 10 AndAlso aspAmino >= 5 Then
                    If ConsumeBasicResources(cell) Then
                        cell.AddMoleculeInternal(MoleculeType.NitrogenSource, -10)
                        cell.AddMoleculeInternal(MoleculeType.AminoMixAspFamily, -5)

                        Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)
                        voxel.HasBiofilm = True
                        voxel.BiofilmStrength = Math.Min(100, voxel.BiofilmStrength + 30)
                    End If
                End If
            End If
        End Sub

        Private Function QuorumProbability(cell As Cell, Optional totalSignal As Integer = -1) As Double
            Const n As Double = 2.0   ' Hill系数
            Const K As Double = 80.0  ' [v2.0] 降低半饱和常数，使群体感应更容易触发

            Dim S As Integer
            If totalSignal >= 0 Then
                S = totalSignal
            Else
                S = cell.GetMoleculeAmount(MoleculeType.SignalMolecule)
            End If

            Return Math.Pow(S, n) / (Math.Pow(K, n) + Math.Pow(S, n))
        End Function
    End Class
End Namespace
