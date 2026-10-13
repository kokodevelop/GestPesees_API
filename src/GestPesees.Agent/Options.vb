Public Class SourceOptions
    ''' <summary>Nom du logiciel/pont (ex. "EditeurA"). Fait partie de la clé d'idempotence.</summary>
    Public Property Name As String = ""
    ''' <summary>"SqlServer" ou "MySql".</summary>
    Public Property Provider As String = "SqlServer"
    Public Property ConnectionString As String = ""
    ''' <summary>
    ''' SELECT sur la base de l'éditeur. Alias attendus : Wm (curseur croissant), SourceId, WeighedAt,
    ''' Plate, Product, ThirdParty, GrossKg, TareKg, NetKg. Toute autre colonne part dans Extra.
    ''' Pas de ORDER BY, pas de ';' final : l'agent ajoute filtre, tri et limite.
    ''' </summary>
    Public Property Query As String = ""
    ''' <summary>"Number" (ID auto-incrémenté) ou "DateTime".</summary>
    Public Property WatermarkType As String = "Number"
    ''' <summary>Point de départ la 1ère fois (évite de reprendre tout l'historique si voulu).</summary>
    Public Property InitialWatermark As String = "0"
End Class

Public Class AgentOptions
    Public Property SiteCode As String = ""
    Public Property SiteName As String = ""
    Public Property ApiUrl As String = ""
    Public Property ApiKey As String = ""
    Public Property PollSeconds As Integer = 60
    Public Property BatchSize As Integer = 200
    Public Property StatePath As String = "state.json"
    Public Property Sources As List(Of SourceOptions) = New List(Of SourceOptions)()
End Class
