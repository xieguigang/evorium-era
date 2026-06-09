Namespace Models

    Public Class Molecule

        Public Property Type As MoleculeType
        Public Property Quantity As Integer = 0

        Sub New(type As MoleculeType)
            Me.Type = type
        End Sub

        Public Overrides Function ToString() As String
            Return Type.Description
        End Function

        Public Shared Function EmptyModel(type As MoleculeType) As Molecule
            Select Case type
                Case MoleculeType.Protein : Return New ProteinMolecule
                Case MoleculeType.DNA : Return New DNAMolecule
                Case Else
                    Return New Molecule(type)
            End Select
        End Function

        Public Shared Operator *(m As Molecule, f As Double) As Double
            Return m.Quantity * f
        End Operator

        Public Shared Operator -(m As Molecule, f As Double) As Double
            Return m.Quantity - f
        End Operator

        Public Shared Operator /(m As Molecule, f As Double) As Double
            Return m.Quantity / f
        End Operator

        Public Shared Operator <(m As Molecule, f As Double) As Boolean
            Return m.Quantity < f
        End Operator

        Public Shared Operator >(m As Molecule, f As Double) As Boolean
            Return m.Quantity > f
        End Operator
    End Class

    Public Class ProteinMolecule : Inherits Molecule

        Public Property Protein As GeneOntology

        Sub New()
            Call MyBase.New(MoleculeType.Protein)
        End Sub

    End Class

    Public Class DNAMolecule : Inherits Molecule

        Public Property Genes As Gene()

        Sub New()
            Call MyBase.New(MoleculeType.DNA)
        End Sub

    End Class

End Namespace
