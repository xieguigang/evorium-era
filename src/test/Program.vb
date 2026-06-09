Imports EvoriumEra

Module Program

    Sub Main(args As String())
        Dim config As Configs = Configs.Default
        Dim simulator As New NaturalEvolution(config, snapshotRoot:=App.HOME & "/output")

        Call simulator.Run(maxSteps:=9)
    End Sub
End Module
