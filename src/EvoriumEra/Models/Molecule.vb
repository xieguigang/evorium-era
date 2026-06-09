Public Class Molecule
    Public Property Type As MoleculeType
    Public Property Quantity As Integer = 0
End Class

Public Class Gene
    Public Property FunctionTag As GeneOntology
    Public Const LengthInNucleotides As Integer = 9
End Class

Public Class Replicon
    Public Property Genes As List(Of Gene) = New List(Of Gene)
    Public Property IsPlasmid As Boolean = False
    Public ReadOnly Property NucleotideLength As Integer
        Get
            Return Genes.Count * Gene.LengthInNucleotides
        End Get
    End Property
End Class