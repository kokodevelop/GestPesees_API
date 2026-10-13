Imports Microsoft.AspNetCore.Mvc
Imports GestPesees.Contracts
Imports GestPesees.Api.Data
Imports GestPesees.Api.Security

Namespace Controllers
    <ApiController>
    <Route("api/v1/pesees")>
    Public Class PeseesController
        Inherits ControllerBase

        Private Const MaxBatchSize As Integer = 1000
        Private ReadOnly _store As IPeseeStore

        Public Sub New(store As IPeseeStore)
            _store = store
        End Sub

        <HttpPost("batch")>
        Public Async Function PostBatch(<FromBody> batch As PeseeBatch) As Task(Of ActionResult(Of PeseeBatchResult))
            Dim site = DirectCast(HttpContext.Items(ApiKeyMiddleware.ItemKey), SiteInfo)

            If Not String.Equals(batch.SiteCode, site.SiteCode, StringComparison.OrdinalIgnoreCase) Then
                Return Forbid()
            End If
            If String.IsNullOrWhiteSpace(batch.Source) Then
                Return BadRequest("Source obligatoire.")
            End If
            If batch.Pesees Is Nothing OrElse batch.Pesees.Count > MaxBatchSize Then
                Return BadRequest($"Lot invalide (maximum {MaxBatchSize} pesées).")
            End If

            Dim valid = batch.Pesees.
                Where(Function(p) Not String.IsNullOrWhiteSpace(p.SourceId) AndAlso p.WeighedAt > DateTime.MinValue).
                ToList()

            If valid.Count > 0 Then
                Await _store.UpsertAsync(site, batch.Source, valid)
            End If

            Dim result As New PeseeBatchResult With {
                .Received = batch.Pesees.Count,
                .Accepted = valid.Count,
                .Rejected = batch.Pesees.Count - valid.Count
            }
            Await _store.LogSyncAsync(site, batch.Source, result)
            Return Ok(result)
        End Function

        ''' <summary>Test de connectivité/clé API depuis l'agent.</summary>
        <HttpGet("ping")>
        Public Function Ping() As ActionResult
            Dim site = DirectCast(HttpContext.Items(ApiKeyMiddleware.ItemKey), SiteInfo)
            Return Ok(New With {site.SiteCode, site.SiteName})
        End Function
    End Class
End Namespace
