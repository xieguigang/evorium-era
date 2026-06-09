Imports System.IO
Imports System.IO.Compression
Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container
Imports Microsoft.VisualBasic.Serialization
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

        Private Shared Sub writeJSON(Of T)(zip As ZipArchive, data As T, file As String)
            Dim entry As ZipArchiveEntry = zip.CreateEntry(file)
            Dim jsonstr As String = data.GetJson

            Using writer = New StreamWriter(entry.Open())
                Call writer.Write(jsonstr)
            End Using
        End Sub

        Public Sub SaveSnapshot(simulation As NaturalEvolution)
            Dim snapshot As (frame As Snapshot, voxels As VoxelSnapshot(), cells As CellSnapshot()) = CreateSnapshot(simulation)
            ' 保存为ZIP
            Dim zipPath = Path.Combine(_basePath, $"iter_{simulation.CurrentIteration:D8}.zip")

            If zipPath.FileExists Then
                Call zipPath.DeleteFile
            End If

            Using zip = ZipFile.Open(zipPath, ZipArchiveMode.Create)
                ' 主快照文件
                Call writeJSON(zip, snapshot.frame, "snapshot.json")
                Call writeJSON(zip, snapshot.voxels, "voxels.json")
                Call writeJSON(zip, snapshot.cells, "cells.json")

                ' 附加元数据文件
                Dim metaEntry = zip.CreateEntry("metadata.txt")

                Using writer = New StreamWriter(metaEntry.Open())
                    writer.WriteLine($"Iteration: {simulation.CurrentIteration}")
                    writer.WriteLine($"Timestamp: {DateTime.Now}")
                    writer.WriteLine($"Cells: {simulation.LivingCellCount}")
                    writer.WriteLine($"DeadCells: {simulation.DeadCellCount}")
                    writer.WriteLine($"CrossFeeding: {simulation.CrossFeedingEvents}")
                    writer.WriteLine($"Temperature: {simulation.AverageTemperature}")
                    writer.WriteLine($"IonStrength: {simulation.AverageIonStrength}")
                    writer.WriteLine($"Denaturation: {simulation.DenaturationEvents}")
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
                            .SnapshotTime = DateTime.Now,
                            .BiofilmStrength = voxel.BiofilmStrength,
                            .ExternalIonStrength = voxel.ExternalIonStrength,
                            .MoleculeDensity = 0,
                            .Temperature = voxel.Temperature
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
                                .PlasmidCount = cell.Plasmids.Count,
                                .Age = cell.Age,
                                .ColdShockMitigation = cell.ColdShockMitigation,
                                .DivisionCount = cell.DivisionCount,
                                .GeneCounts = cell.GetTotalGenes,
                                .Generation = cell.Generation,
                                .Genome = cell.Genome.Clone,
                                .InternalIonStrength = cell.InternalIonStrength,
                                .OsmoticState = cell.OsmoticState,
                                .ParentID = cell.ParentID,
                                .Plasmids = cell.Plasmids.Select(Function(r) r.Clone).ToArray,
                                .ProteinActivityFactor = cell.ProteinActivityFactor,
                                .Proteins = New Dictionary(Of GeneOntology, Integer)(cell.Proteins)
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