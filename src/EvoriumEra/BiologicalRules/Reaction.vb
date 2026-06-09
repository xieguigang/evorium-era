Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules

    Public Class Reaction

        Public Property Substrates As (mol As MoleculeType, n As Integer)()
        Public Property Products As (mol As MoleculeType, n As Integer)()
        Public Property Enzyme As GeneOntology
        Public Property ATP As Integer
        Public Property ExemptATP As Boolean
        Public Property Inhibitors As (mol As MoleculeType, n As Integer)()

        Sub New(enzyme As GeneOntology, energy As Integer, exemptATP As Boolean)
            _Enzyme = enzyme
            _ExemptATP = exemptATP
            ATP = energy
        End Sub

        Public Function Left(ParamArray Substrates As (mol As MoleculeType, n As Integer)()) As Reaction
            Me.Substrates = Substrates.Select(Function(a) (a.mol, Math.Abs(a.n))).ToArray
            Return Me
        End Function

        Public Function Right(ParamArray Products As (mol As MoleculeType, n As Integer)()) As Reaction
            Me.Products = Products
            Return Me
        End Function

        Public Function Inhibit(ParamArray Effectors As (mol As MoleculeType, n As Integer)()) As Reaction
            Me.Inhibitors = Effectors
            Return Me
        End Function

        Public Sub Execute(cell As Cell)
            If cell.HasFunction(Enzyme) Then
                Dim left = Substrates.Select(Function(si) (si.mol, si.n, current:=cell.GetMoleculeAmount(si.mol))).ToList

                For Each item In left
                    If item.current < item.n Then
                        Return
                    End If
                Next

                If Not Inhibitors Is Nothing Then
                    For Each item In Inhibitors
                        If cell.GetMoleculeAmount(item.mol) > item.n Then
                            Return
                        End If
                    Next
                End If

                If IBiochemicalRule.ConsumeBasicResources(cell, ExemptATP) Then
                    For Each item In left
                        Call cell.AddMoleculeInternal(item.mol, -item.n)
                    Next
                    For Each item In Products
                        Call cell.AddMoleculeInternal(item.mol, item.n)
                    Next

                    If ATP <> 0 Then
                        cell.ATP = Math.Min(cell.ATP + ATP, 1000)
                    End If
                End If
            End If
        End Sub
    End Class
End Namespace