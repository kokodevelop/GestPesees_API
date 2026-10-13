Imports System.Net.Http
Imports System.Net.Http.Json
Imports GestPesees.Contracts

Public Class ApiClient
    Private ReadOnly _http As HttpClient

    Public Sub New(opt As AgentOptions)
        _http = New HttpClient With {
            .BaseAddress = New Uri(opt.ApiUrl.TrimEnd("/"c) & "/"),
            .Timeout = TimeSpan.FromSeconds(60)
        }
        _http.DefaultRequestHeaders.Add("X-Api-Key", opt.ApiKey)
    End Sub

    ''' <summary>Lève une exception si l'API ne répond pas 2xx : le curseur n'avance alors pas.</summary>
    Public Async Function SendAsync(batch As PeseeBatch, ct As Threading.CancellationToken) As Task(Of PeseeBatchResult)
        Using resp = Await _http.PostAsJsonAsync("api/v1/pesees/batch", batch, ct)
            resp.EnsureSuccessStatusCode()
            Return Await resp.Content.ReadFromJsonAsync(Of PeseeBatchResult)(cancellationToken:=ct)
        End Using
    End Function
End Class
