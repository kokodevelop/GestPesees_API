Imports System.IO
Imports System.Text.Json

''' <summary>Dernier curseur envoyé avec succès, par source (fichier JSON local).</summary>
Public Class WatermarkStore
    Private ReadOnly _path As String
    Private _values As Dictionary(Of String, String)

    Public Sub New(path As String)
        _path = path
        _values = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        If File.Exists(path) Then
            Dim loaded = JsonSerializer.Deserialize(Of Dictionary(Of String, String))(File.ReadAllText(path))
            If loaded IsNot Nothing Then
                _values = New Dictionary(Of String, String)(loaded, StringComparer.OrdinalIgnoreCase)
            End If
        End If
    End Sub

    Public Function [Get](source As String, initial As String) As String
        Dim v As String = Nothing
        Return If(_values.TryGetValue(source, v), v, initial)
    End Function

    ''' <summary>Écriture atomique (fichier temporaire puis remplacement).</summary>
    Public Sub [Set](source As String, value As String)
        _values(source) = value
        Dim tmp = _path & ".tmp"
        File.WriteAllText(tmp, JsonSerializer.Serialize(_values))
        File.Move(tmp, _path, True)
    End Sub
End Class
