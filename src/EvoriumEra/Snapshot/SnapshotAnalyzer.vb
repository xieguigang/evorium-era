Imports System.IO
Imports System.IO.Compression
Imports EvoriumEra.Models
Imports Microsoft.VisualBasic.Serialization.JSON

Namespace Data

    Public Class SnapshotAnalyzer
        ''' <summary>
        ''' 提取某个分子在整个时间序列上的变化
        ''' </summary>
        Public Function ExtractTimeSeries(snapshotFolder As String,
                                     moleculeType As MoleculeType,
                                     Optional voxelFilter As Func(Of VoxelSnapshot, Boolean) = Nothing) As List(Of (Long, Double))

            Dim results = New List(Of (Long, Double))
            Dim files = Directory.GetFiles(snapshotFolder, "iter_*.zip").OrderBy(Function(f) f)

            For Each file In files
                Using zip = ZipFile.OpenRead(file)
                    Dim entry = zip.GetEntry("snapshot.json")
                    Dim entry2 = zip.GetEntry("voxels.json")
                    Dim entry3 = zip.GetEntry("cells.json")
                    Dim meta As Snapshot

                    Using reader = New StreamReader(entry.Open)
                        Dim json = reader.ReadToEnd()
                        meta = json.LoadJSON(Of Snapshot)
                    End Using

                    Using reader = New StreamReader(entry2.Open())
                        Dim json = reader.ReadToEnd()
                        Dim snapshot = json.LoadJSON(Of VoxelSnapshot())
                        Dim voxels = If(voxelFilter Is Nothing,
                                   snapshot,
                                   snapshot.Where(voxelFilter))

                        Dim total = voxels.Sum(Function(v)
                                                   If v.ExternalMolecules.ContainsKey(moleculeType) Then
                                                       Return v.ExternalMolecules(moleculeType)
                                                   Else
                                                       Return 0
                                                   End If
                                               End Function)

                        results.Add((meta.Iteration, total))
                    End Using
                End Using
            Next

            Return results
        End Function
    End Class
End Namespace