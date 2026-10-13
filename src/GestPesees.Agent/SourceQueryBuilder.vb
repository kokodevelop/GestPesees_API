''' <summary>
''' Enveloppe la requête de l'éditeur : filtre sur le curseur, tri croissant, limite de lot.
''' Les paramètres sont @Watermark et @BatchSize.
''' </summary>
Public Module SourceQueryBuilder
    Public Function Build(provider As String, query As String) As String
        Dim inner = query.Trim().TrimEnd(";"c)
        Select Case provider.ToLowerInvariant()
            Case "sqlserver"
                Return $"SELECT TOP (@BatchSize) * FROM ({inner}) AS q WHERE q.Wm > @Watermark ORDER BY q.Wm"
            Case "mysql"
                Return $"SELECT * FROM ({inner}) AS q WHERE q.Wm > @Watermark ORDER BY q.Wm LIMIT @BatchSize"
            Case Else
                Throw New NotSupportedException($"Provider inconnu : {provider}")
        End Select
    End Function
End Module
