Imports Microsoft.VisualBasic.Linq

Namespace Models

    ''' <summary>
    ''' 基因：组成复制子的基础功能单元
    ''' </summary>
    Public Class Gene

        Public Property FunctionOntology As GeneOntology

        Public Const LengthInNucleotides As Integer = 9

        Sub New()
        End Sub

        Sub New(clone As Gene)
            FunctionOntology = clone.FunctionOntology
        End Sub

        Public Overrides Function ToString() As String
            Return FunctionOntology.Description
        End Function

    End Class

    ''' <summary>
    ''' 复制子：一个复制子就是若干个基因的集合
    ''' </summary>
    Public Class Replicon : Implements Enumeration(Of String)

        Public Property Genes As New List(Of Gene)
        Public Property IsPlasmid As Boolean = False

        Public ReadOnly Property NucleotideLength As Integer
            Get
                Return Genes.Count * Gene.LengthInNucleotides
            End Get
        End Property

        Public ReadOnly Property Size As Integer
            Get
                Return Genes.Count
            End Get
        End Property

        Public Function Clone() As Replicon
            Return New Replicon With {
                .Genes = New List(Of Gene)(From g As Gene
                                           In Genes
                                           Select New Gene(clone:=g)),
                .IsPlasmid = IsPlasmid
            }
        End Function

        ''' <summary>
        ''' Display the gene function list
        ''' </summary>
        ''' <returns></returns>
        Public Overrides Function ToString() As String
            Return Genes.Select(Function(g) g.ToString).OrderBy(Function(s) s).JoinBy("; ")
        End Function

        Public Iterator Function GenericEnumerator() As IEnumerator(Of String) Implements Enumeration(Of String).GenericEnumerator
            For Each gene As Gene In Genes
                Yield gene.ToString
            Next
        End Function
    End Class
End Namespace