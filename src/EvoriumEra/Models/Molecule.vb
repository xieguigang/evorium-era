Namespace Models

    ''' <summary>
    ''' 基础分子对象，包括小分子代谢物和生物大分子
    ''' </summary>
    Public Class Molecule

        Public Property Type As MoleculeType
        Public Overridable ReadOnly Property Quantity As Integer = 0

        Sub New(type As MoleculeType)
            Me.Type = type
        End Sub

        Public Overridable Sub SetQuantity(x As Integer)
            _Quantity = x
        End Sub

        Public Sub AddQuantity(x As Integer)
            Call SetQuantity(Quantity + x)
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

    ''' <summary>
    ''' 主要用于存储环境中的蛋白质对象，这些蛋白质对象在环境中可能还会继续发挥功能活性
    ''' </summary>
    Public Class ProteinMolecule : Inherits Molecule

        ''' <summary>
        ''' proteins in environment
        ''' </summary>
        ''' <returns></returns>
        Public Property Proteins As New Dictionary(Of GeneOntology, List(Of ExtracellularProtein))

        Public Overrides ReadOnly Property Quantity As Integer
            Get
                Return Aggregate prot In Proteins.Values Into Sum(prot.Count)
            End Get
        End Property

        Sub New()
            Call MyBase.New(MoleculeType.Protein)
        End Sub

        Public Sub Add(protein As GeneOntology, num As Integer)
            If Not Proteins.ContainsKey(protein) Then
                Proteins(protein) = New List(Of ExtracellularProtein)
            End If

            For i As Integer = 1 To num
                Proteins(protein).Add(New ExtracellularProtein With {.Term = protein})
            Next
        End Sub

        Public Overrides Sub SetQuantity(x As Integer)
            Throw New InvalidProgramException($"protein molecule can not be set quantity directly!")
        End Sub

    End Class

    Public Class ExtracellularProtein

        Public Property Term As GeneOntology

        ''' <summary>
        ''' 这个蛋白质在外部环境中能够发挥生物学活性的剩余时间。当这个属性值小于等于零的时候，将会失活
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' 默认只能够在环境中保持5个循环周期的生物学活性
        ''' </remarks>
        Public Property ViabilityDuration As Integer = 5

    End Class

    ''' <summary>
    ''' 主要用于表示释放到环境之中或者被细胞内吞到细胞内的外源DNA片段
    ''' 
    ''' 1. 细胞死亡裂解后，DNA片段释放到环境中，可能会被环境中有活性的裂解酶降解掉，或者等待被其他的细胞吞噬
    ''' 2. 细胞内吞环境中的DNA后，在细胞内这些外源DNA片段有两个命运：被整合到基因组上或者被裂解掉
    ''' </summary>
    Public Class DNAMolecule : Inherits Molecule

        Public Property DNAFragments As New List(Of Replicon)

        Public Overrides ReadOnly Property Quantity As Integer
            Get
                Return DNAFragments.Count
            End Get
        End Property

        Sub New()
            Call MyBase.New(MoleculeType.DNA)
        End Sub

        Public Sub Add(fragment As Replicon)
            DNAFragments.Add(fragment)
        End Sub

        Public Overrides Sub SetQuantity(x As Integer)
            Throw New InvalidProgramException($"external DNA fragment molecule can not be set quantity directly!")
        End Sub
    End Class

End Namespace
