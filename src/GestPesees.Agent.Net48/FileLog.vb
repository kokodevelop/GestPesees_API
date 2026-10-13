Imports System.IO

''' <summary>Journal fichier quotidien (logs\agent-AAAAMMJJ.log), conservé 30 jours, + console en mode interactif.</summary>
Public Class FileLog
    Implements IAgentLog

    Private ReadOnly _dir As String
    Private ReadOnly _lock As New Object()
    Private _lastPurge As Date = Date.MinValue

    Public Sub New()
        _dir = Path.Combine(AppContext.BaseDirectory, "logs")
        Directory.CreateDirectory(_dir)
    End Sub

    Public Sub Info(message As String) Implements IAgentLog.Info
        Write("INFO ", message)
    End Sub

    Public Sub [Error](message As String, ex As Exception) Implements IAgentLog.Error
        Write("ERROR", message & If(ex Is Nothing, "", " | " & ex.ToString()))
    End Sub

    Private Sub Write(level As String, message As String)
        Dim line = $"{Date.Now:yyyy-MM-dd HH:mm:ss} {level} {message}"
        If Environment.UserInteractive Then Console.WriteLine(line)
        SyncLock _lock
            Try
                File.AppendAllText(Path.Combine(_dir, $"agent-{Date.Now:yyyyMMdd}.log"), line & Environment.NewLine)
                Purge()
            Catch
                ' Un journal qui échoue ne doit jamais arrêter la collecte.
            End Try
        End SyncLock
    End Sub

    Private Sub Purge()
        If (Date.Now - _lastPurge).TotalHours < 24 Then Return
        _lastPurge = Date.Now
        For Each f In Directory.GetFiles(_dir, "agent-*.log")
            If File.GetLastWriteTime(f) < Date.Now.AddDays(-30) Then File.Delete(f)
        Next
    End Sub
End Class
