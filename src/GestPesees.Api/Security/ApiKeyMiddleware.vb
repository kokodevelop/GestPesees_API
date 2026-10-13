Imports System.Security.Cryptography
Imports System.Text
Imports Microsoft.AspNetCore.Http
Imports GestPesees.Api.Data

Namespace Security
    ''' <summary>
    ''' Authentifie chaque requête par l'en-tête X-Api-Key. La clé identifie le site :
    ''' le SiteCode d'un lot ne peut jamais être usurpé.
    ''' </summary>
    Public Class ApiKeyMiddleware
        Public Const HeaderName As String = "X-Api-Key"
        Public Const ItemKey As String = "Site"

        Private ReadOnly _next As RequestDelegate
        Private ReadOnly _sites As ISiteDirectory

        Public Sub New(next_ As RequestDelegate, sites As ISiteDirectory)
            _next = next_
            _sites = sites
        End Sub

        Public Async Function InvokeAsync(context As HttpContext) As Task
            Dim key = context.Request.Headers(HeaderName).ToString()
            If String.IsNullOrWhiteSpace(key) Then
                context.Response.StatusCode = StatusCodes.Status401Unauthorized
                Return
            End If

            Dim site = Await _sites.FindByKeyHashAsync(Hash(key))
            If site Is Nothing Then
                context.Response.StatusCode = StatusCodes.Status401Unauthorized
                Return
            End If

            context.Items(ItemKey) = site
            Await _next(context)
        End Function

        Public Shared Function Hash(key As String) As String
            Dim bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key))
            Return Convert.ToHexString(bytes).ToLowerInvariant()
        End Function
    End Class
End Namespace
