Imports System.Data.Common
Imports System.Globalization
Imports Microsoft.Data.SqlClient
Imports MySqlConnector
Imports GestPesees.Contracts

Public Class ReadResult
    Public Property Pesees As List(Of PeseeDto) = New List(Of PeseeDto)()
    ''' <summary>Curseur de la dernière ligne lue, Nothing si aucune ligne.</summary>
    Public Property NewWatermark As String
End Class

Public Class SourceReader
    Implements ISourceReader

    Private Shared ReadOnly Known As New HashSet(Of String)(
        {"wm", "sourceid", "weighedat", "plate", "product", "thirdparty", "grosskg", "tarekg", "netkg"},
        StringComparer.OrdinalIgnoreCase)

    Public Async Function ReadBatchAsync(src As SourceOptions, watermark As String, batchSize As Integer,
                                         ct As Threading.CancellationToken) As Task(Of ReadResult) _
        Implements ISourceReader.ReadBatchAsync
        Dim result As New ReadResult()
        Using cn As DbConnection = CreateConnection(src)
            Await cn.OpenAsync(ct)
            Using cmd As DbCommand = cn.CreateCommand()
                cmd.CommandText = SourceQueryBuilder.Build(src.Provider, src.Query)
                cmd.CommandTimeout = 60
                AddParam(cmd, "@Watermark", ParseWatermark(src.WatermarkType, watermark))
                AddParam(cmd, "@BatchSize", batchSize)

                Using rd As DbDataReader = Await cmd.ExecuteReaderAsync(ct)
                    While Await rd.ReadAsync(ct)
                        result.Pesees.Add(MapRow(rd))
                        result.NewWatermark = FormatWatermark(src.WatermarkType, rd("Wm"))
                    End While
                End Using
            End Using
        End Using
        Return result
    End Function

    Private Shared Function CreateConnection(src As SourceOptions) As DbConnection
        Select Case src.Provider.ToLowerInvariant()
            Case "sqlserver" : Return New SqlConnection(src.ConnectionString)
            Case "mysql" : Return New MySqlConnection(src.ConnectionString)
            Case Else : Throw New NotSupportedException($"Provider inconnu : {src.Provider}")
        End Select
    End Function

    Private Shared Sub AddParam(cmd As DbCommand, name As String, value As Object)
        Dim p = cmd.CreateParameter()
        p.ParameterName = name
        p.Value = value
        cmd.Parameters.Add(p)
    End Sub

    Public Shared Function ParseWatermark(type As String, value As String) As Object
        If String.Equals(type, "DateTime", StringComparison.OrdinalIgnoreCase) Then
            Return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        End If
        Return Decimal.Parse(value, CultureInfo.InvariantCulture)
    End Function

    Public Shared Function FormatWatermark(type As String, value As Object) As String
        If String.Equals(type, "DateTime", StringComparison.OrdinalIgnoreCase) Then
            Return Convert.ToDateTime(value, CultureInfo.InvariantCulture).ToString("o", CultureInfo.InvariantCulture)
        End If
        Return Convert.ToDecimal(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
    End Function

    Private Shared Function MapRow(rd As DbDataReader) As PeseeDto
        Dim p As New PeseeDto()
        For i = 0 To rd.FieldCount - 1
            If rd.IsDBNull(i) Then Continue For
            Dim name = rd.GetName(i)
            Dim v = rd.GetValue(i)
            Select Case name.ToLowerInvariant()
                Case "wm" ' curseur : non envoyé
                Case "sourceid" : p.SourceId = Convert.ToString(v, CultureInfo.InvariantCulture)
                Case "weighedat" : p.WeighedAt = Convert.ToDateTime(v, CultureInfo.InvariantCulture)
                Case "plate" : p.Plate = Convert.ToString(v, CultureInfo.InvariantCulture)
                Case "product" : p.Product = Convert.ToString(v, CultureInfo.InvariantCulture)
                Case "thirdparty" : p.ThirdParty = Convert.ToString(v, CultureInfo.InvariantCulture)
                Case "grosskg" : p.GrossKg = Convert.ToDecimal(v, CultureInfo.InvariantCulture)
                Case "tarekg" : p.TareKg = Convert.ToDecimal(v, CultureInfo.InvariantCulture)
                Case "netkg" : p.NetKg = Convert.ToDecimal(v, CultureInfo.InvariantCulture)
                Case Else
                    If Not TypeOf v Is Byte() Then
                        p.Extra(name) = Convert.ToString(v, CultureInfo.InvariantCulture)
                    End If
            End Select
        Next
        Return p
    End Function
End Class
