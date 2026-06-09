Imports System.Text
Imports EvoriumEra.Models

Namespace Data

    Public Class VoxelSnapshot
        ' 位置信息
        Public Property X As Integer
        Public Property Y As Integer
        Public Property Z As Integer

        ' 外部分子浓度
        Public Property ExternalMolecules As Dictionary(Of MoleculeType, Integer)

        ' 物理状态
        Public Property HasBiofilm As Boolean
        Public Property BiofilmStrength As Integer = 0

        ' 占据情况
        Public Property OccupantCellId As Guid?
        Public Property OccupantCellAlive As Boolean?

        ' 统计信息
        Public Property TotalMolecules As Integer
        Public Property MoleculeDensity As Double
        Public Property Temperature As Double = 25.0
        Public Property ExternalIonStrength As Double

        Public Property PH As Double

        ' 时间戳
        Public Property SnapshotTime As DateTime

        ' 转换为可读字符串（用于调试）
        Public Overrides Function ToString() As String
            Dim sb As New StringBuilder()
            sb.AppendLine($"Voxel({X},{Y},{Z})")
            sb.AppendLine($"  Biofilm: {HasBiofilm}")
            sb.AppendLine($"  Occupied: {If(OccupantCellId.HasValue, OccupantCellId.ToString(), "Empty")}")
            sb.AppendLine($"  Total Molecules: {TotalMolecules}")

            If ExternalMolecules IsNot Nothing Then
                For Each kvp In ExternalMolecules.Where(Function(kv) kv.Value > 0)
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}")
                Next
            End If

            Return sb.ToString()
        End Function
    End Class
End Namespace