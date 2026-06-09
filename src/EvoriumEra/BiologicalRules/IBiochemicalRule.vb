Public Interface IBiochemicalRule

    ''' <summary>
    ''' 该规则负责处理的基因功能
    ''' </summary>
    ReadOnly Property SupportedFunctions As List(Of GeneOntology)

    Sub Execute(cell As Cell, env As Environment3D, rng As Random)

End Interface