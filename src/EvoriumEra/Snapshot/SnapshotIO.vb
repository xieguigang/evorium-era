Imports System.IO
Imports System.IO.Compression
Imports Microsoft.VisualBasic.Serialization.JSON

Namespace Data

    Public Module SnapshotIO
        Public Sub SaveToZip(snapshot As Snapshot, path As String)
            Dim json = snapshot.GetJson
            Using zip = ZipFile.Open(path, ZipArchiveMode.Create)
                Dim entry = zip.CreateEntry($"iteration_{snapshot.Iteration}.json")
                Using writer = New StreamWriter(entry.Open())
                    writer.Write(json)
                End Using
            End Using
        End Sub
    End Module
End Namespace