''' <summary>
''' Une pesée au format commun, quel que soit l'éditeur du logiciel de pont bascule.
''' </summary>
Public Class PeseeDto
    ''' <summary>Identifiant de la pesée dans la base source (n° de ticket, ID...). Obligatoire.</summary>
    Public Property SourceId As String = ""
    ''' <summary>Date/heure de la pesée, heure locale du site.</summary>
    Public Property WeighedAt As DateTime
    Public Property Plate As String
    Public Property Product As String
    Public Property ThirdParty As String
    Public Property GrossKg As Decimal?
    Public Property TareKg As Decimal?
    Public Property NetKg As Decimal?
    ''' <summary>Colonnes supplémentaires de la source, non mappées sur le modèle commun.</summary>
    Public Property Extra As Dictionary(Of String, String) = New Dictionary(Of String, String)()
End Class

''' <summary>Lot de pesées envoyé par l'agent d'un site.</summary>
Public Class PeseeBatch
    Public Property SiteCode As String = ""
    Public Property SiteName As String = ""
    ''' <summary>Nom du logiciel/pont source (un site peut en avoir plusieurs).</summary>
    Public Property Source As String = ""
    Public Property Pesees As List(Of PeseeDto) = New List(Of PeseeDto)()
End Class

Public Class PeseeBatchResult
    Public Property Received As Integer
    Public Property Accepted As Integer
    Public Property Rejected As Integer
End Class
