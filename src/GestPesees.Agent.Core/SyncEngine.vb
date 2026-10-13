Imports System.IO
Imports System.Threading
Imports GestPesees.Contracts

''' <summary>
''' Cœur commun aux deux agents : pour chaque source, lit les pesées au-delà du curseur, les envoie
''' à l'API, et ne fait avancer le curseur qu'après un accusé de réception. La base (ou le dossier)
''' de l'éditeur sert donc de file d'attente : coupure réseau = on reprend au même point.
''' </summary>
Public Class SyncEngine
    Private ReadOnly _opt As AgentOptions
    Private ReadOnly _log As IAgentLog
    Private ReadOnly _dbReader As New SourceReader()
    Private ReadOnly _csvReader As New CsvSourceReader()
    Private ReadOnly _api As ApiClient
    Private ReadOnly _state As WatermarkStore

    Public Sub New(opt As AgentOptions, log As IAgentLog)
        _opt = opt
        _log = log
        _api = New ApiClient(opt)
        _state = New WatermarkStore(Path.Combine(AppContext.BaseDirectory, opt.StatePath))
    End Sub

    ''' <summary>Boucle de synchronisation jusqu'à l'annulation.</summary>
    Public Async Function RunAsync(ct As CancellationToken) As Task
        While Not ct.IsCancellationRequested
            Await RunCycleAsync(ct)
            Try
                Await Task.Delay(TimeSpan.FromSeconds(_opt.PollSeconds), ct)
            Catch ex As OperationCanceledException
                Return
            End Try
        End While
    End Function

    ''' <summary>Un passage sur toutes les sources ; l'échec d'une source n'arrête pas les autres.</summary>
    Public Async Function RunCycleAsync(ct As CancellationToken) As Task
        For Each src In _opt.Sources
            Try
                Await SyncSourceAsync(src, ct)
            Catch ex As OperationCanceledException When ct.IsCancellationRequested
                Return
            Catch ex As Exception
                _log.Error($"Source {src.Name} : échec, nouvel essai au prochain cycle", ex)
            End Try
        Next
    End Function

    Private Function ReaderFor(src As SourceOptions) As ISourceReader
        If String.Equals(src.Provider, "Csv", StringComparison.OrdinalIgnoreCase) Then Return _csvReader
        Return _dbReader
    End Function

    Private Async Function SyncSourceAsync(src As SourceOptions, ct As CancellationToken) As Task
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
            _log.Info($"Source {src.Name} : {res.Accepted}/{res.Received} pesées acceptées")

            If read.Pesees.Count < _opt.BatchSize Then Return
        Loop
    End Function
End Class
