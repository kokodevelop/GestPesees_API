Public Class SourceOptions
    ''' <summary>Nom du logiciel/pont (ex. "EditeurA"). Fait partie de la clé d'idempotence.</summary>
    Public Property Name As String = ""
    ''' <summary>"SqlServer", "MySql" ou "Csv".</summary>
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

    ' --- Provider "Csv" : source alternative par dossier d'export (Query/ConnectionString ignorés) ---
    ''' <summary>Dossier où le logiciel de pesée dépose ses exports.</summary>
    Public Property Folder As String = ""
    Public Property FilePattern As String = "*.csv"
    Public Property Delimiter As String = ";"
    ''' <summary>Ex. "utf-8" ou "windows-1252".</summary>
    Public Property Encoding As String = "utf-8"
    Public Property HasHeader As Boolean = True
    ''' <summary>Culture pour les dates, ex. "fr-FR".</summary>
    Public Property Culture As String = "fr-FR"
    ''' <summary>Format de date exact optionnel, ex. "dd/MM/yyyy HH:mm:ss".</summary>
    Public Property DateFormat As String = ""
    ''' <summary>"," (français) ou ".".</summary>
    Public Property DecimalSeparator As String = ","
    ''' <summary>
    ''' Nom standard -> nom de colonne CSV (ou numéro de colonne base 0 sans en-tête). SourceId et
    ''' WeighedAt sont obligatoires ; Plate, Product, ThirdParty, GrossKg, TareKg, NetKg optionnels.
    ''' </summary>
    Public Property ColumnMap As Dictionary(Of String, String) = New Dictionary(Of String, String)()
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
