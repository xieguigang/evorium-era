Imports EvoriumEra.Models
Imports EvoriumEra.Models.Container

Namespace BiologicalRules.Rules

    ''' <summary>
    ''' [v3.0] 离子转运规则
    ''' 
    ''' 处理细胞与环境之间的离子交换：
    ''' - 被动扩散：小离子（Na+, K+, Cl-）沿浓度梯度扩散
    ''' - 主动转运：需要PhosphateTransport基因转运磷酸盐
    ''' - 铁离子摄取：通过铁载体介导
    ''' 
    ''' 离子不仅是渗透压因子，也是代谢辅因子：
    ''' - Mg2+：ATP酶辅因子，缺乏时ATP产率降低
    ''' - Fe2+/Fe3+：电子传递链，铁呼吸
    ''' - Ca2+：信号转导
    ''' - PO4 3-：核苷酸和ATP合成必需
    ''' </summary>
    Public Class IonTransportRule : Inherits IBiochemicalRule

        Sub New()
            Call MyBase.New(GeneOntology.PhosphateTransport, GeneOntology.IronRedox)
        End Sub

        Public Overrides Sub Execute(cell As Cell, env As NaturalEnvironment)
            Dim voxel = env.Grid(cell.Position.X, cell.Position.Y, cell.Position.Z)

            ' ===== 被动离子扩散 =====
            ' Na+, K+, Cl- 沿浓度梯度被动扩散
            Dim passiveIons = {MoleculeType.SodiumIon, MoleculeType.PotassiumIon, MoleculeType.ChlorideIon}
            For Each ion In passiveIons
                Dim internalAmt = cell.GetMoleculeAmount(ion)
                Dim externalAmt = voxel.GetMoleculeAmount(ion)
                Dim diff = externalAmt - internalAmt

                If Math.Abs(diff) > 2 Then
                    Dim transfer = CInt(Math.Sign(diff) * Math.Min(Math.Abs(diff) * 0.1, 3))
                    If transfer <> 0 Then
                        cell.AddMoleculeInternal(ion, transfer)
                        If Not voxel.ExternalMolecules.ContainsKey(ion) Then
                            voxel.ExternalMolecules(ion) = Molecule.EmptyModel(ion)
                        End If
                        voxel.ExternalMolecules(ion).AddQuantity(-transfer)
                        If voxel.ExternalMolecules(ion) < 0 Then
                            voxel.ExternalMolecules(ion).SetQuantity(0)
                        End If
                    End If
                End If
            Next

            ' ===== 磷酸盐主动转运 =====
            If cell.HasFunction(GeneOntology.PhosphateTransport) Then
                Dim extPhosphate = voxel.GetMoleculeAmount(MoleculeType.Phosphate)
                If extPhosphate > 0 AndAlso cell.ATP >= 1 Then
                    Dim uptake = Math.Min(extPhosphate, 3)
                    cell.ATP -= 1
                    cell.AddMoleculeInternal(MoleculeType.Phosphate, uptake)
                    If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.Phosphate) Then
                        voxel.ExternalMolecules(MoleculeType.Phosphate) = Molecule.EmptyModel(MoleculeType.Phosphate)
                    End If
                    voxel.ExternalMolecules(MoleculeType.Phosphate).AddQuantity(-uptake)
                    If voxel.ExternalMolecules(MoleculeType.Phosphate) < 0 Then
                        voxel.ExternalMolecules(MoleculeType.Phosphate).SetQuantity(0)
                    End If
                End If
            End If

            ' ===== 铁离子摄取 =====
            ' 通过铁载体介导的铁摄取
            Dim extSiderophore = voxel.GetMoleculeAmount(MoleculeType.Siderophore)
            Dim extFe3 = voxel.GetMoleculeAmount(MoleculeType.IronIII)
            If extSiderophore > 0 AndAlso extFe3 > 0 Then
                ' 铁载体-Fe3+复合物被摄取
                Dim uptake = Math.Min(Math.Min(extSiderophore, extFe3), 2)
                cell.AddMoleculeInternal(MoleculeType.IronIII, uptake)
                cell.AddMoleculeInternal(MoleculeType.Siderophore, uptake)
                If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.IronIII) Then
                    voxel.ExternalMolecules(MoleculeType.IronIII) = Molecule.EmptyModel(MoleculeType.IronIII)
                End If
                voxel.ExternalMolecules(MoleculeType.IronIII).AddQuantity(-uptake)
                If voxel.ExternalMolecules(MoleculeType.IronIII) < 0 Then
                    voxel.ExternalMolecules(MoleculeType.IronIII).SetQuantity(0)
                End If
            End If

            ' ===== 铁氧化还原 =====
            If cell.HasFunction(GeneOntology.IronRedox) Then
                ' Fe3+ → Fe2+（铁还原，厌氧呼吸）
                Dim fe3 = cell.GetMoleculeAmount(MoleculeType.IronIII)
                If fe3 > 0 AndAlso cell.GetMoleculeAmount(MoleculeType.Oxygen) < 5 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.IronIII, -1)
                        cell.AddMoleculeInternal(MoleculeType.IronII, 1)
                        cell.ATP = Math.Min(cell.ATP + 3, 1000)
                    End If
                End If

                ' Fe2+ → Fe3+（铁氧化，好氧条件下产能）
                Dim fe2 = cell.GetMoleculeAmount(MoleculeType.IronII)
                Dim o2 = cell.GetMoleculeAmount(MoleculeType.Oxygen)
                If fe2 > 0 AndAlso o2 > 5 Then
                    If ConsumeBasicResources(cell, exemptATP:=True) Then
                        cell.AddMoleculeInternal(MoleculeType.IronII, -1)
                        cell.AddMoleculeInternal(MoleculeType.Oxygen, -1)
                        cell.AddMoleculeInternal(MoleculeType.IronIII, 1)
                        cell.ATP = Math.Min(cell.ATP + 2, 1000)
                    End If
                End If
            End If

            ' ===== Mg2+ 被动扩散（微量） =====
            Dim extMg = voxel.GetMoleculeAmount(MoleculeType.MagnesiumIon)
            Dim intMg = cell.GetMoleculeAmount(MoleculeType.MagnesiumIon)
            If extMg > intMg + 2 Then
                cell.AddMoleculeInternal(MoleculeType.MagnesiumIon, 1)
                If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.MagnesiumIon) Then
                    voxel.ExternalMolecules(MoleculeType.MagnesiumIon) = Molecule.EmptyModel(MoleculeType.MagnesiumIon)
                End If
                voxel.ExternalMolecules(MoleculeType.MagnesiumIon).AddQuantity(-1)
            End If

            ' ===== Ca2+ 被动扩散（微量） =====
            Dim extCa = voxel.GetMoleculeAmount(MoleculeType.CalciumIon)
            Dim intCa = cell.GetMoleculeAmount(MoleculeType.CalciumIon)
            If extCa > intCa + 2 Then
                cell.AddMoleculeInternal(MoleculeType.CalciumIon, 1)
                If Not voxel.ExternalMolecules.ContainsKey(MoleculeType.CalciumIon) Then
                    voxel.ExternalMolecules(MoleculeType.CalciumIon) = Molecule.EmptyModel(MoleculeType.CalciumIon)
                End If
                voxel.ExternalMolecules(MoleculeType.CalciumIon).AddQuantity(-1)
            End If
        End Sub
    End Class
End Namespace
