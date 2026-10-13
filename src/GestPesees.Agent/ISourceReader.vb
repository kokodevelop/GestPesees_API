''' <summary>Lit un lot de pesées au-delà du curseur d'une source (base de données ou dossier CSV).</summary>
Public Interface ISourceReader
    Function ReadBatchAsync(src As SourceOptions, watermark As String, batchSize As Integer,
                            ct As Threading.CancellationToken) As Task(Of ReadResult)
End Interface
