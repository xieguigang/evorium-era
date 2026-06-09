Imports Microsoft.VisualBasic.Imaging

Namespace Models.Container

    Public Class Voxel : Implements IVoxel

        Public Property Position As SpatialIndex3D Implements IVoxel.Position
        Public Property ExternalMolecules As New Dictionary(Of MoleculeType, Molecule) Implements IVoxel.Molecules

        Public Property Occupant As Cell = Nothing
        Public Property HasBiofilm As Boolean = False

        ''' <summary>
        ''' [v2.0] 生物膜强度（0-100），影响物质交换阻断程度
        ''' </summary>
        ''' <returns></returns>
        Public Property BiofilmStrength As Integer = 0

        ' [v3.0] 温度
        ''' <summary>当前格子温度（°C），受环境基线、代谢产热、热扩散影响</summary>
        Public Property Temperature As Double = 25.0

        ' [v3.0] 环境离子强度
        ''' <summary>环境离子强度（mM当量），影响细胞渗透压</summary>
        Public ReadOnly Property ExternalIonStrength As Double
            Get
                Return CalculateExternalIonStrength()
            End Get
        End Property

        Sub New()
        End Sub

        Sub New(x As Integer, y As Integer, z As Integer)
            Position = New SpatialIndex3D(x, y, z)
        End Sub

        ''' <summary>
        ''' [v2.0] 获取格子内指定分子的数量
        ''' </summary>
        Public Function GetMoleculeAmount(type As MoleculeType) As Integer
            If ExternalMolecules.ContainsKey(type) Then
                Return ExternalMolecules(type).Quantity
            Else
                Return 0
            End If
        End Function

        ''' <summary>
        ''' [v3.0] 计算环境离子强度
        ''' </summary>
        Private Function CalculateExternalIonStrength() As Double
            Dim ionContributions As Double = 0.0

            ionContributions += GetMoleculeAmount(MoleculeType.SodiumIon) * 1.0
            ionContributions += GetMoleculeAmount(MoleculeType.PotassiumIon) * 1.0
            ionContributions += GetMoleculeAmount(MoleculeType.ChlorideIon) * 1.0
            ionContributions += GetMoleculeAmount(MoleculeType.HydrogenIon) * 1.0
            ionContributions += GetMoleculeAmount(MoleculeType.CalciumIon) * 4.0
            ionContributions += GetMoleculeAmount(MoleculeType.MagnesiumIon) * 4.0
            ionContributions += GetMoleculeAmount(MoleculeType.IronII) * 4.0
            ionContributions += GetMoleculeAmount(MoleculeType.Sulfate) * 4.0
            ionContributions += GetMoleculeAmount(MoleculeType.IronIII) * 9.0

            Return ionContributions * 0.5
        End Function

        Public Overrides Function ToString() As String
            Return Position.ToString
        End Function
    End Class
End Namespace
