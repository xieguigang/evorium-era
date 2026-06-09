Imports System.IO
Imports System.IO.Compression
Imports Microsoft.VisualBasic.Serialization.JSON

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
                Using reader = New StreamReader(entry.Open())
                    Dim json = reader.ReadToEnd()
                    Dim snapshot = json.LoadJSON(Of Snapshot)

                    Dim voxels = If(voxelFilter Is Nothing,
                                   snapshot.Voxels,
                                   snapshot.Voxels.Where(voxelFilter))

                    Dim total = voxels.Sum(Function(v)
                                               If v.ExternalMolecules.ContainsKey(moleculeType) Then
                                                   Return v.ExternalMolecules(moleculeType)
                                               Else
                                                   Return 0
                                               End If
                                           End Function)

                    results.Add((snapshot.Iteration, total))
                End Using
            End Using
        Next

        Return results
    End Function
End Class