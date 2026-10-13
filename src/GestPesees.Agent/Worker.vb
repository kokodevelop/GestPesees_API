Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Options

''' <summary>Hôte .NET 8 : délègue toute la logique au SyncEngine commun.</summary>
Public Class Worker
    Inherits BackgroundService

    Private ReadOnly _engine As SyncEngine

    Public Sub New(opt As IOptions(Of AgentOptions), log As ILogger(Of Worker))
        _engine = New SyncEngine(opt.Value, New MsLogAdapter(log))
    End Sub

    Protected Overrides Function ExecuteAsync(stoppingToken As Threading.CancellationToken) As Task
        Return _engine.RunAsync(stoppingToken)
    End Function

    Private Class MsLogAdapter
        Implements IAgentLog

        Private ReadOnly _log As ILogger

        Public Sub New(log As ILogger)
            _log = log
        End Sub

        Public Sub Info(message As String) Implements IAgentLog.Info
            _log.LogInformation("{Message}", message)
        End Sub

        Public Sub [Error](message As String, ex As Exception) Implements IAgentLog.Error
            _log.LogError(ex, "{Message}", message)
        End Sub
    End Class
End Class
