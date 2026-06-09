Imports System.IO
Imports System.IO.Compression
Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports Microsoft.VisualBasic.Serialization.JSON

Namespace Data

    Public Class SnapshotManager

        Private _basePath As String

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="basePath">A temp dir path</param>
        Public Sub New(basePath As String)
            _basePath = basePath
            Directory.CreateDirectory(_basePath)
        End Sub

        Public Sub SaveSnapshot(simulation As NaturalEvolution)
            Dim snapshot As (frame As Snapshot, voxels As VoxelSnapshot(), cells As CellSnapshot()) = CreateSnapshot(simulation)
            Dim json As String = snapshot.frame.GetJson

            ' 保存为ZIP
            Dim zipPath = Path.Combine(_basePath, $"iter_{simulation.CurrentIteration:D8}.zip")

            Using zip = ZipFile.Open(zipPath, ZipArchiveMode.Create)
                ' 主快照文件
                Dim entry = zip.CreateEntry("snapshot.json")
                Using writer = New StreamWriter(entry.Open())
                    writer.Write(json)
                End Using

                entry = zip.CreateEntry("voxels.json")

                Using writer = New StreamWriter(entry.Open())
                    writer.Write(snapshot.voxels.GetJson)
                End Using

                entry = zip.CreateEntry("cells.json")

                Using writer = New StreamWriter(entry.Open())
                    writer.Write(snapshot.cells.GetJson)
                End Using

                ' 附加元数据文件
                Dim metaEntry = zip.CreateEntry("metadata.txt")
                Using writer = New StreamWriter(metaEntry.Open())
                    writer.WriteLine($"Iteration: {simulation.CurrentIteration}")
                    writer.WriteLine($"Timestamp: {DateTime.Now}")
                    writer.WriteLine($"Cells: {simulation.LivingCellCount}")
                End Using
            End Using
        End Sub

        Private Function CreateSnapshot(simulation As NaturalEvolution) As (Snapshot, VoxelSnapshot(), CellSnapshot())
            Dim snapshot As New Snapshot With {
                .Iteration = simulation.CurrentIteration,
                .Timestamp = DateTime.Now,
                .EnvironmentDimensions = simulation.Env.Dimensions
            }
            Dim voxels As New List(Of VoxelSnapshot)
            Dim cells As New List(Of CellSnapshot)

            ' 遍历所有体素
            For x As Integer = 0 To simulation.Env.Dimensions.Width - 1
                For y As Integer = 0 To simulation.Env.Dimensions.Height - 1
                    For z As Integer = 0 To simulation.Env.Dimensions.Depth - 1
                        Dim voxel = simulation.Env.Grid(x, y, z)
                        ' 创建体素快照
                        Dim voxelSnap As New VoxelSnapshot With {
                            .X = x, .Y = y, .Z = z,
                            .ExternalMolecules = New Dictionary(Of MoleculeType, Integer)(voxel.ExternalMolecules),
                            .HasBiofilm = voxel.HasBiofilm,
                            .OccupantCellId = If(voxel.Occupant?.ID, Nothing),
                            .OccupantCellAlive = If(voxel.Occupant?.IsAlive, Nothing),
                            .TotalMolecules = voxel.ExternalMolecules.Values.Sum(),
                            .SnapshotTime = DateTime.Now
                        }
                        voxels.Add(voxelSnap)

                        ' 创建细胞快照
                        If voxel.Occupant IsNot Nothing Then
                            Dim cell = voxel.Occupant
                            Dim cellSnap As New CellSnapshot With {
                                .ID = cell.ID,
                                .Position = cell.Position,
                                .IsAlive = cell.IsAlive,
                                .HasCellWall = cell.HasCellWall,
                                .InternalMolecules = New Dictionary(Of MoleculeType, Integer)(cell.InternalMolecules),
                                .TotalMolecules = cell.TotalMolecules,
                                .ATP = cell.ATP,
                                .GenomeSize = cell.Genome.NucleotideLength,
                                .PlasmidCount = cell.Plasmids.Count
                            }
                            cells.Add(cellSnap)
                        End If
                    Next
                Next
            Next

            Return (snapshot, voxels.ToArray, cells.ToArray)
        End Function
    End Class
End Namespace