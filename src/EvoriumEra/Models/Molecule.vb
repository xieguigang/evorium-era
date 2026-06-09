Namespace Models

    Public Class Molecule

        Public Property Type As MoleculeType
        Public Property Quantity As Integer = 0

        Public Overrides Function ToString() As String
            Return Type.Description
        End Function
    End Class

    Public Class ProteinMolecule : Inherits Molecule

        Public Property Protein As GeneOntology

    End Class

    Public Class DNAMolecule : Inherits Molecule

        Public Property Genes As Gene()

    End Class

End Namespace
