Imports EvoriumEra

Module Program

    Sub Main(args As String())
        Dim config As Configs = Configs.Default
        Dim simulator As New NaturalEvolution(config, snapshotRoot:="Z:/output", debug:=True)

        Call simulator.Initialize()
        Call simulator.Run(maxSteps:=100)
    End Sub
End Module
