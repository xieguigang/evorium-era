Namespace Models

    Public Class Molecule

        Public Property Type As MoleculeType
        Public Property Quantity As Integer = 0

        Public Overrides Function ToString() As String
            Return Type.Description
        End Function
    End Class

    Public Class Gene

        ''' <summary>
        ''' gene function ontology in this simulation system
        ''' </summary>
        ''' <returns></returns>
        Public Property FunctionOntology As GeneOntology

        Public Const LengthInNucleotides As Integer = 9

        Public Overrides Function ToString() As String
            Return FunctionOntology.Description
        End Function

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
End Namespace