Namespace BiologicalRules

    Public Interface IBiochemicalRule

        ''' <summary>
        ''' 该规则负责处理的基因功能
        ''' </summary>
        ReadOnly Property SupportedFunctions As GeneOntology()

        Sub Execute(cell As Cell, env As Environment3D)

    End Interface
End Namespace