Imports EvoriumEra.Models

Namespace BiologicalRules

    Public Interface IEnvironmentRule

        Sub ExecuteEnvironment(env As NaturalEnvironment, iteration As Long)

    End Interface
End Namespace