Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Options
Imports GestPesees.Contracts

''' <summary>
''' Boucle principale : pour chaque source, lit les pesées au-delà du curseur, les envoie à l'API,
''' et ne fait avancer le curseur qu'après un accusé de réception. La base de l'éditeur sert donc
''' de file d'attente : coupure réseau = on reprend au même point, sans perte ni doublon.
''' </summary>
Public Class Worker
    Inherits BackgroundService

    Private ReadOnly _opt As AgentOptions
    Private ReadOnly _log As ILogger(Of Worker)
    Private ReadOnly _dbReader As New SourceReader()
    Private ReadOnly _csvReader As New CsvSourceReader()
    Private ReadOnly _api As ApiClient
    Private ReadOnly _state As WatermarkStore

    Public Sub New(opt As IOptions(Of AgentOptions), log As ILogger(Of Worker))
        _opt = opt.Value
        _log = log
        _api = New ApiClient(_opt)
        _state = New WatermarkStore(IO.Path.Combine(AppContext.BaseDirectory, _opt.StatePath))
    End Sub

    Protected Overrides Async Function ExecuteAsync(stoppingToken As Threading.CancellationToken) As Task
        While Not stoppingToken.IsCancellationRequested
            For Each src In _opt.Sources
                Try
                    Await SyncSourceAsync(src, stoppingToken)
                Catch ex As OperationCanceledException When stoppingToken.IsCancellationRequested
                    Exit While
                Catch ex As Exception
                    _log.LogError(ex, "Source {Source} : échec, nouvel essai au prochain cycle", src.Name)
                End Try
            Next
            Try
                Await Task.Delay(TimeSpan.FromSeconds(_opt.PollSeconds), stoppingToken)
            Catch ex As OperationCanceledException
                Exit While
            End Try
        End While
    End Function

    Private Function ReaderFor(src As SourceOptions) As ISourceReader
        If String.Equals(src.Provider, "Csv", StringComparison.OrdinalIgnoreCase) Then Return _csvReader
        Return _dbReader
    End Function

    Private Async Function SyncSourceAsync(src As SourceOptions, ct As Threading.CancellationToken) As Task
        Do
            Dim wm = _state.Get(src.Name, src.InitialWatermark)
            Dim read = Await ReaderFor(src).ReadBatchAsync(src, wm, _opt.BatchSize, ct)
            If read.Pesees.Count = 0 Then Return

            Dim batch As New PeseeBatch With {
                .SiteCode = _opt.SiteCode,
                .SiteName = _opt.SiteName,
                .Source = src.Name,
                .Pesees = read.Pesees
            }
            Dim res = Await _api.SendAsync(batch, ct)
            _state.Set(src.Name, read.NewWatermark)
            _log.LogInformation("Source {Source} : {Accepted}/{Received} pesées acceptées", src.Name, res.Accepted, res.Received)

            If read.Pesees.Count < _opt.BatchSize Then Return
        Loop
    End Function
End Class
