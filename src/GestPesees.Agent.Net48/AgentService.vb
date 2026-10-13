Imports System.ServiceProcess
Imports System.Threading
Imports Microsoft.Extensions.Configuration

''' <summary>Service Windows (.NET Framework 4.8) enveloppant le SyncEngine commun.</summary>
Public Class AgentService
    Inherits ServiceBase

    Public Const Name As String = "GestPesees Agent"

    Private _cts As CancellationTokenSource
    Private _task As Task
    Private ReadOnly _log As IAgentLog = New FileLog()

    Public Sub New()
        ServiceName = Name
        CanStop = True
    End Sub

    Protected Overrides Sub OnStart(args As String())
        StartLoop()
    End Sub

    Protected Overrides Sub OnStop()
        StopLoop()
    End Sub

    ''' <summary>Démarre la boucle (appelé aussi par le mode console).</summary>
    Public Sub StartLoop()
        Try
            Dim opt = LoadOptions()
            Dim engine As New SyncEngine(opt, _log)
            _cts = New CancellationTokenSource()
            Dim token = _cts.Token
            _task = Task.Run(Function() engine.RunAsync(token))
            _log.Info($"Agent démarré pour le site {opt.SiteCode} ({opt.Sources.Count} source(s))")
        Catch ex As Exception
            _log.Error("Démarrage impossible (vérifier appsettings.json)", ex)
            Throw
        End Try
    End Sub

    Public Sub StopLoop()
        If _cts Is Nothing Then Return
        _cts.Cancel()
        Try
            _task.Wait(TimeSpan.FromSeconds(20))
        Catch ex As AggregateException
            ' annulation attendue
        End Try
        _log.Info("Agent arrêté")
    End Sub

    Private Shared Function LoadOptions() As AgentOptions
        ' En service, le répertoire courant est System32 : on part du dossier de l'exécutable.
        Dim config = New ConfigurationBuilder().
            SetBasePath(AppContext.BaseDirectory).
            AddJsonFile("appsettings.json", optional:=False, reloadOnChange:=False).
            Build()
        Dim opt = config.GetSection("Agent").Get(Of AgentOptions)()
        If opt Is Nothing OrElse opt.Sources.Count = 0 Then
            Throw New InvalidOperationException("Section Agent absente ou sans source dans appsettings.json")
        End If
        Return opt
    End Function
End Class
