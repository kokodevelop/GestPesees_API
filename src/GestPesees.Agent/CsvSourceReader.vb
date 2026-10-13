Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Text.Json
Imports GestPesees.Contracts

''' <summary>Progression dans un fichier CSV : nombre de lignes de données déjà envoyées.</summary>
Public Class CsvFileState
    Public Property Count As Integer
    ''' <summary>Première ligne de données : si elle change, le fichier a été remplacé et on repart de 0.</summary>
    Public Property Head As String = ""
End Class

''' <summary>
''' Source alternative pour les éditeurs qui refusent l'accès à leur base : le logiciel exporte
''' des CSV dans un dossier, l'agent les lit. Le « curseur » est un JSON {fichier: progression},
''' ce qui gère aussi bien un fichier par pesée qu'un fichier journalier complété au fil de l'eau.
''' </summary>
Public Class CsvSourceReader
    Implements ISourceReader

    Shared Sub New()
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
    End Sub

    Public Function ReadBatchAsync(src As SourceOptions, watermark As String, batchSize As Integer,
                                   ct As Threading.CancellationToken) As Task(Of ReadResult) _
        Implements ISourceReader.ReadBatchAsync
        Return Task.FromResult(ReadBatch(src, watermark, batchSize))
    End Function

    Public Shared Function ReadBatch(src As SourceOptions, watermark As String, batchSize As Integer) As ReadResult
        If Not Directory.Exists(src.Folder) Then
            Throw New DirectoryNotFoundException($"Dossier CSV introuvable : {src.Folder}")
        End If

        Dim state = LoadState(watermark)
        Dim result As New ReadResult()
        Dim enc = Encoding.GetEncoding(src.Encoding)
        Dim delimiter = If(String.IsNullOrEmpty(src.Delimiter), ";"c, src.Delimiter(0))

        Dim files = Directory.GetFiles(src.Folder, src.FilePattern).
            OrderBy(Function(f) f, StringComparer.OrdinalIgnoreCase).ToList()

        ' Oublie les fichiers supprimés/archivés pour que le curseur ne grossisse pas indéfiniment.
        Dim present = New HashSet(Of String)(files.Select(Function(f) Path.GetFileName(f)), StringComparer.OrdinalIgnoreCase)
        For Each k In state.Keys.Where(Function(n) Not present.Contains(n)).ToList()
            state.Remove(k)
        Next

        For Each f In files
            If result.Pesees.Count >= batchSize Then Exit For
            Dim name = Path.GetFileName(f)
            Dim records = ReadRecords(f, enc, delimiter)

            Dim header As String() = Nothing
            If src.HasHeader Then
                If records.Count = 0 Then Continue For
                header = records(0)
                records.RemoveAt(0)
            End If

            Dim fs As CsvFileState = Nothing
            If Not state.TryGetValue(name, fs) Then fs = New CsvFileState()
            Dim head = If(records.Count > 0, String.Join(delimiter, records(0)), "")
            If fs.Count > records.Count OrElse (fs.Count > 0 AndAlso fs.Head <> head) Then
                fs.Count = 0 ' fichier remplacé ou tronqué : on renvoie (l'API dédoublonne)
            End If
            fs.Head = head

            Dim map = ResolveMap(src, header, name)
            While fs.Count < records.Count AndAlso result.Pesees.Count < batchSize
                result.Pesees.Add(MapRecord(records(fs.Count), header, map, src))
                fs.Count += 1
            End While
            state(name) = fs
        Next

        If result.Pesees.Count > 0 Then result.NewWatermark = JsonSerializer.Serialize(state)
        Return result
    End Function

    Private Shared Function LoadState(watermark As String) As Dictionary(Of String, CsvFileState)
        If Not String.IsNullOrWhiteSpace(watermark) AndAlso watermark.TrimStart().StartsWith("{") Then
            Dim d = JsonSerializer.Deserialize(Of Dictionary(Of String, CsvFileState))(watermark)
            If d IsNot Nothing Then Return New Dictionary(Of String, CsvFileState)(d, StringComparer.OrdinalIgnoreCase)
        End If
        Return New Dictionary(Of String, CsvFileState)(StringComparer.OrdinalIgnoreCase)
    End Function

    Private Shared Function ReadRecords(path As String, enc As Encoding, delimiter As Char) As List(Of String())
        ' FileShare.ReadWrite : le logiciel de pesée peut être en train d'écrire dans le fichier.
        Using stream As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Using sr As New StreamReader(stream, enc)
                Return ParseRecords(sr.ReadToEnd(), delimiter)
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Analyse CSV (guillemets, séparateurs et retours à la ligne dans les champs). Une ligne non
    ''' terminée par un saut de ligne est en cours d'écriture : elle est ignorée jusqu'au prochain cycle.
    ''' </summary>
    Public Shared Function ParseRecords(text As String, delimiter As Char) As List(Of String())
        Dim records As New List(Of String())
        Dim fields As New List(Of String)
        Dim sb As New StringBuilder()
        Dim inQuotes = False
        Dim i = 0
        While i < text.Length
            Dim c = text(i)
            If inQuotes Then
                If c = """"c Then
                    If i + 1 < text.Length AndAlso text(i + 1) = """"c Then
                        sb.Append(""""c)
                        i += 1
                    Else
                        inQuotes = False
                    End If
                Else
                    sb.Append(c)
                End If
            ElseIf c = """"c Then
                inQuotes = True
            ElseIf c = delimiter Then
                fields.Add(sb.ToString())
                sb.Clear()
            ElseIf c = ControlChars.Cr Then
                ' ignoré : \r\n et \n sont traités pareil
            ElseIf c = ControlChars.Lf Then
                fields.Add(sb.ToString())
                sb.Clear()
                If Not (fields.Count = 1 AndAlso fields(0).Length = 0) Then records.Add(fields.ToArray())
                fields.Clear()
            Else
                sb.Append(c)
            End If
            i += 1
        End While
        Return records
    End Function

    Private Shared Function ResolveMap(src As SourceOptions, header As String(), fileName As String) As Dictionary(Of String, Integer)
        Dim map As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        For Each kv In src.ColumnMap
            Dim idx As Integer
            If header IsNot Nothing Then
                idx = Array.FindIndex(header, Function(h) String.Equals(h.Trim(), kv.Value.Trim(), StringComparison.OrdinalIgnoreCase))
                If idx < 0 Then
                    Throw New InvalidOperationException($"Colonne '{kv.Value}' introuvable dans {fileName}")
                End If
            ElseIf Not Integer.TryParse(kv.Value, idx) Then
                Throw New InvalidOperationException($"ColumnMap '{kv.Key}' : sans en-tête, indiquer un n° de colonne (base 0)")
            End If
            map(kv.Key) = idx
        Next
        If Not map.ContainsKey("SourceId") OrElse Not map.ContainsKey("WeighedAt") Then
            Throw New InvalidOperationException("ColumnMap doit contenir au minimum SourceId et WeighedAt")
        End If
        Return map
    End Function

    Private Shared Function MapRecord(rec As String(), header As String(), map As Dictionary(Of String, Integer),
                                      src As SourceOptions) As PeseeDto
        Dim culture = CultureInfo.GetCultureInfo(If(String.IsNullOrWhiteSpace(src.Culture), "fr-FR", src.Culture))
        Dim p As New PeseeDto()
        p.SourceId = Cell(rec, map, "SourceId")
        p.Plate = Cell(rec, map, "Plate")
        p.Product = Cell(rec, map, "Product")
        p.ThirdParty = Cell(rec, map, "ThirdParty")
        p.GrossKg = ParseDecimal(Cell(rec, map, "GrossKg"), src.DecimalSeparator)
        p.TareKg = ParseDecimal(Cell(rec, map, "TareKg"), src.DecimalSeparator)
        p.NetKg = ParseDecimal(Cell(rec, map, "NetKg"), src.DecimalSeparator)

        ' Date illisible : on laisse la valeur par défaut, l'API rejette la ligne (visible dans SyncLog)
        ' plutôt que de bloquer définitivement la source sur une ligne défectueuse.
        Dim d As DateTime
        Dim raw = Cell(rec, map, "WeighedAt")
        Dim ok = If(String.IsNullOrWhiteSpace(src.DateFormat),
                    DateTime.TryParse(raw, culture, DateTimeStyles.None, d),
                    DateTime.TryParseExact(raw, src.DateFormat, culture, DateTimeStyles.None, d))
        If ok Then p.WeighedAt = d

        If header IsNot Nothing Then
            Dim used = New HashSet(Of Integer)(map.Values)
            For i = 0 To Math.Min(header.Length, rec.Length) - 1
                If Not used.Contains(i) AndAlso rec(i).Length > 0 Then p.Extra(header(i).Trim()) = rec(i)
            Next
        End If
        Return p
    End Function

    Private Shared Function Cell(rec As String(), map As Dictionary(Of String, Integer), key As String) As String
        Dim idx As Integer
        If map.TryGetValue(key, idx) AndAlso idx >= 0 AndAlso idx < rec.Length Then
            Dim v = rec(idx).Trim()
            If v.Length > 0 Then Return v
        End If
        Return Nothing
    End Function

    Private Shared Function ParseDecimal(s As String, decimalSeparator As String) As Decimal?
        If String.IsNullOrWhiteSpace(s) Then Return Nothing
        Dim t = s.Replace(" ", "").Replace(ChrW(160), "")
        If decimalSeparator = "," Then
            t = t.Replace(".", "").Replace(",", ".")
        Else
            t = t.Replace(",", "")
        End If
        Dim v As Decimal
        If Decimal.TryParse(t, NumberStyles.Number Or NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, v) Then Return v
        Return Nothing
    End Function
End Class
