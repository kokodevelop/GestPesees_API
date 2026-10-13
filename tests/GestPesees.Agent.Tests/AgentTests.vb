Imports System.IO
Imports Xunit
Imports GestPesees.Agent

Public Class AgentTests
    <Fact>
    Public Sub SqlServer_utilise_TOP()
        Dim sql = SourceQueryBuilder.Build("SqlServer", "SELECT id AS Wm FROM t;")
        Assert.Equal("SELECT TOP (@BatchSize) * FROM (SELECT id AS Wm FROM t) AS q WHERE q.Wm > @Watermark ORDER BY q.Wm", sql)
    End Sub

    <Fact>
    Public Sub MySql_utilise_LIMIT()
        Dim sql = SourceQueryBuilder.Build("mysql", "SELECT id AS Wm FROM t")
        Assert.EndsWith("ORDER BY q.Wm LIMIT @BatchSize", sql)
    End Sub

    <Fact>
    Public Sub Provider_inconnu_leve_une_exception()
        Assert.Throws(Of NotSupportedException)(Function() SourceQueryBuilder.Build("oracle", "SELECT 1"))
    End Sub

    <Fact>
    Public Sub Watermark_numerique_aller_retour()
        Dim s = SourceReader.FormatWatermark("Number", 42)
        Assert.Equal(42D, CDec(SourceReader.ParseWatermark("Number", s)))
    End Sub

    <Fact>
    Public Sub Watermark_date_aller_retour()
        Dim d As New DateTime(2026, 3, 1, 8, 15, 30, 123)
        Dim s = SourceReader.FormatWatermark("DateTime", d)
        Assert.Equal(d, CDate(SourceReader.ParseWatermark("DateTime", s)))
    End Sub

    <Fact>
    Public Sub WatermarkStore_persiste()
        Dim path = IO.Path.Combine(IO.Path.GetTempPath(), Guid.NewGuid().ToString() & ".json")
        Try
            Dim a As New WatermarkStore(path)
            Assert.Equal("0", a.Get("A", "0"))
            a.Set("A", "17")
            Assert.Equal("17", New WatermarkStore(path).Get("A", "0"))
        Finally
            If File.Exists(path) Then File.Delete(path)
        End Try
    End Sub
End Class
