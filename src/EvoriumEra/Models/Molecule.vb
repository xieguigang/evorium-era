Namespace Models

    Public Class Molecule

        Public Property Type As MoleculeType
        Public Property Quantity As Integer = 0

        Public Overrides Function ToString() As String
            Return Type.Description
        End Function
    End Class

    ''' <summary>
    ''' 基因： 组成复制子的基础功能单元，在这里我们并不关心基因在复制子上的具体位置信息，而是重点关注基因的功能信息。在这个模拟程序中，目前我将基因设定为一个功能词条
    ''' </summary>
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

    ''' <summary>
    ''' 复制子： 一个复制子就是若干个基因的集合，在这里复制子包括基因组序列和质粒序列
    ''' </summary>
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