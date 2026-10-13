''' <summary>Journal minimal, adapté par chaque hôte (ILogger en .NET 8, fichier en .NET Framework).</summary>
Public Interface IAgentLog
    Sub Info(message As String)
    Sub [Error](message As String, ex As Exception)
End Interface
