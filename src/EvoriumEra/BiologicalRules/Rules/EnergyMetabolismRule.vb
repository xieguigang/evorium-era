Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v2.0 改进] 能量代谢规则
    ''' 
    ''' 好氧：1 glucose + 1 O2 → 12 ATP + 6 CO2
    ''' 厌氧：1 acetate → 5 ATP + 2 CO2 + 2 H+（无氧时）
    ''' 
    ''' 改进：需要对应蛋白质才能执行
    ''' </summary>
    Public Class EnergyMetabolismRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(GeneOntology.AerobicEnergyMetabolismATP, GeneOntology.AnaerobicEnergyMetabolismATP)
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            ' 需氧能量代谢
            If cell.HasFunction(GeneOntology.AerobicEnergyMetabolismATP) Then
                Dim glucose = cell.GetMoleculeAmount(MoleculeType.Glucose)
                Dim oxygen = cell.GetMoleculeAmount(MoleculeType.Oxygen)

                If glucose > 0 AndAlso oxygen > 0 Then
                    ' 能量代谢豁免ATP消耗
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.Glucose, -1)
                        cell.AddMoleculeInternal(MoleculeType.Oxygen, -1)
                        cell.ATP = Math.Min(cell.ATP + 12, 1000)
                        env.AddMolecule(cell, MoleculeType.CarbonDioxide, 6)
                    End If
                End If
            End If

            ' 厌氧能量代谢（有氧时抑制）
            If cell.HasFunction(GeneOntology.AnaerobicEnergyMetabolismATP) Then
                Dim acetate = cell.GetMoleculeAmount(MoleculeType.Acetate)
                Dim oxygen = cell.GetMoleculeAmount(MoleculeType.Oxygen)

                If acetate > 0 AndAlso oxygen < 10 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.Acetate, -1)
                        cell.ATP = Math.Min(cell.ATP + 5, 1000)
                        env.AddMolecule(cell, MoleculeType.CarbonDioxide, 2)
                        env.AddMolecule(cell, MoleculeType.HydrogenIon, 2)
                    End If
                End If

                Dim carbon = cell.GetMoleculeAmount(MoleculeType.CarbonSource)
                oxygen = cell.GetMoleculeAmount(MoleculeType.Oxygen)

                If carbon > 0 AndAlso oxygen < 10 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.CarbonSource, -1)
                        cell.ATP = Math.Min(cell.ATP + 3, 1000)
                        env.AddMolecule(cell, MoleculeType.CarbonDioxide, 1)
                        env.AddMolecule(cell, MoleculeType.HydrogenIon, 1)
                    End If
                End If
            End If
        End Sub
    End Class
End Namespace
