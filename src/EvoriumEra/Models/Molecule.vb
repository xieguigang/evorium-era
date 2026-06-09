Namespace Models

    Public Class Molecule

        Public Property Type As MoleculeType
        Public Property Quantity As Integer = 0

        Public Overrides Function ToString() As String
            Return Type.Description
        End Function
    End Class

    ''' <summary>
    ''' 基因：组成复制子的基础功能单元
    ''' </summary>
    Public Class Gene

        Public Property FunctionOntology As GeneOntology

        Public Const LengthInNucleotides As Integer = 9

        Public Overrides Function ToString() As String
            Return FunctionOntology.Description
        End Function

    End Class

    ''' <summary>
    ''' 复制子：一个复制子就是若干个基因的集合
    ''' </summary>
    Public Class Replicon

        Public Property Genes As New List(Of Gene)
        Public Property IsPlasmid As Boolean = False

        Public ReadOnly Property NucleotideLength As Integer
            Get
                Return Genes.Count * Gene.LengthInNucleotides
            End Get
        End Property
    End Class
End Namespace
