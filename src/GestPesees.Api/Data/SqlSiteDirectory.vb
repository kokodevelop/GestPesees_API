Imports Microsoft.Extensions.Configuration
Imports Dapper
Imports Microsoft.Data.SqlClient
Imports GestPesees.Api.Security

Namespace Data
    Public Interface ISiteDirectory
        Function FindByKeyHashAsync(keyHash As String) As Task(Of SiteInfo)
    End Interface

    Public Class SqlSiteDirectory
        Implements ISiteDirectory

        Private ReadOnly _cs As String

        Public Sub New(config As IConfiguration)
            _cs = config.GetConnectionString("Central")
        End Sub

        Public Async Function FindByKeyHashAsync(keyHash As String) As Task(Of SiteInfo) _
            Implements ISiteDirectory.FindByKeyHashAsync
            Using cn As New SqlConnection(_cs)
                Return Await cn.QuerySingleOrDefaultAsync(Of SiteInfo)(
                    "SELECT SiteCode, SiteName FROM dbo.Site WHERE ApiKeyHash = @keyHash AND IsActive = 1",
                    New With {keyHash})
            End Using
        End Function
    End Class
End Namespace
