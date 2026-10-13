Imports Microsoft.Extensions.Configuration
Imports System.Text.Json
Imports Dapper
Imports Microsoft.Data.SqlClient
Imports GestPesees.Contracts
Imports GestPesees.Api.Security

Namespace Data
    Public Interface IPeseeStore
        Function UpsertAsync(site As SiteInfo, source As String, pesees As IReadOnlyList(Of PeseeDto)) As Task
        Function LogSyncAsync(site As SiteInfo, source As String, result As PeseeBatchResult) As Task
    End Interface

    Public Class SqlPeseeStore
        Implements IPeseeStore

        Private Const UpsertSql As String = "
MERGE dbo.Pesee WITH (HOLDLOCK) AS t
USING (SELECT @SiteCode AS SiteCode, @Source AS Source, @SourceId AS SourceId) AS s
   ON t.SiteCode = s.SiteCode AND t.Source = s.Source AND t.SourceId = s.SourceId
WHEN MATCHED THEN UPDATE SET
     SiteName = @SiteName, WeighedAt = @WeighedAt, Plate = @Plate, Product = @Product,
     ThirdParty = @ThirdParty, GrossKg = @GrossKg, TareKg = @TareKg, NetKg = @NetKg,
     ExtraJson = @ExtraJson, UpdatedUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT
     (SiteCode, SiteName, Source, SourceId, WeighedAt, Plate, Product, ThirdParty, GrossKg, TareKg, NetKg, ExtraJson)
     VALUES (@SiteCode, @SiteName, @Source, @SourceId, @WeighedAt, @Plate, @Product, @ThirdParty, @GrossKg, @TareKg, @NetKg, @ExtraJson);"

        Private ReadOnly _cs As String

        Public Sub New(config As IConfiguration)
            _cs = config.GetConnectionString("Central")
        End Sub

        Public Async Function UpsertAsync(site As SiteInfo, source As String, pesees As IReadOnlyList(Of PeseeDto)) As Task _
            Implements IPeseeStore.UpsertAsync
            Dim rows = pesees.Select(Function(p) New With {
                .SiteCode = site.SiteCode,
                .SiteName = site.SiteName,
                .Source = source,
                .SourceId = p.SourceId,
                .WeighedAt = p.WeighedAt,
                .Plate = p.Plate,
                .Product = p.Product,
                .ThirdParty = p.ThirdParty,
                .GrossKg = p.GrossKg,
                .TareKg = p.TareKg,
                .NetKg = p.NetKg,
                .ExtraJson = If(p.Extra IsNot Nothing AndAlso p.Extra.Count > 0, JsonSerializer.Serialize(p.Extra), Nothing)
            }).ToList()

            Using cn As New SqlConnection(_cs)
                Await cn.OpenAsync()
                Using tx = cn.BeginTransaction()
                    Await cn.ExecuteAsync(UpsertSql, rows, tx)
                    Await cn.ExecuteAsync("UPDATE dbo.Site SET LastSyncUtc = SYSUTCDATETIME() WHERE SiteCode = @SiteCode",
                                          New With {site.SiteCode}, tx)
                    tx.Commit()
                End Using
            End Using
        End Function

        Public Async Function LogSyncAsync(site As SiteInfo, source As String, result As PeseeBatchResult) As Task _
            Implements IPeseeStore.LogSyncAsync
            Using cn As New SqlConnection(_cs)
                Await cn.ExecuteAsync(
                    "INSERT dbo.SyncLog(SiteCode, Source, Received, Accepted, Rejected) VALUES (@SiteCode, @Source, @Received, @Accepted, @Rejected)",
                    New With {site.SiteCode, source, result.Received, result.Accepted, result.Rejected})
            End Using
        End Function
    End Class
End Namespace
