Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports GestPesees.Contracts

Public Class ApiClient
    Private Shared ReadOnly JsonOptions As New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True}
    Private ReadOnly _http As HttpClient

    Public Sub New(opt As AgentOptions)
        _http = New HttpClient With {
            .BaseAddress = New Uri(opt.ApiUrl.TrimEnd("/"c) & "/"),
            .Timeout = TimeSpan.FromSeconds(60)
        }
        _http.DefaultRequestHeaders.Add("X-Api-Key", opt.ApiKey)
    End Sub

    ''' <summary>Lève une exception si l'API ne répond pas 2xx : le curseur n'avance alors pas.</summary>
    Public Async Function SendAsync(batch As PeseeBatch, ct As CancellationToken) As Task(Of PeseeBatchResult)
        Dim json = JsonSerializer.Serialize(batch)
        Using content As New StringContent(json, Encoding.UTF8, "application/json")
            Using resp = Await _http.PostAsync("api/v1/pesees/batch", content, ct)
                resp.EnsureSuccessStatusCode()
                Dim body = Await resp.Content.ReadAsStringAsync()
                Return JsonSerializer.Deserialize(Of PeseeBatchResult)(body, JsonOptions)
            End Using
        End Using
    End Function
End Class
