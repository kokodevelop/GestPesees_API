Imports System.IO
Imports System.Text
Imports Xunit
Imports GestPesees.Agent

Public Class CsvSourceTests
    Private Shared Function NewFolder() As String
        Dim dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
        Directory.CreateDirectory(dir)
        Return dir
    End Function

    Private Shared Function Options(folder As String) As SourceOptions
        Dim o As New SourceOptions With {.Name = "C", .Provider = "Csv", .Folder = folder, .DateFormat = "dd/MM/yyyy HH:mm:ss"}
        o.ColumnMap("SourceId") = "Ticket"
        o.ColumnMap("WeighedAt") = "Date"
        o.ColumnMap("Plate") = "Immat"
        o.ColumnMap("NetKg") = "Net"
        Return o
    End Function

    <Fact>
    Public Sub ParseRecords_gere_guillemets_et_ligne_incomplete()
        Dim text = "a;b" & vbCrLf & """x;1"";""il dit """"oui""""""" & vbCrLf & "inachevee;"
        Dim recs = CsvSourceReader.ParseRecords(text, ";"c)
        Assert.Equal(2, recs.Count)
        Assert.Equal("x;1", recs(1)(0))
        Assert.Equal("il dit ""oui""", recs(1)(1))
    End Sub

    <Fact>
    Public Sub ReadBatch_mappe_les_colonnes_et_garde_le_reste_en_extra()
        Dim dir = NewFolder()
        Try
            File.WriteAllText(Path.Combine(dir, "a.csv"),
                "Ticket;Date;Immat;Net;Chauffeur" & vbLf & "T1;01/03/2026 08:15:00;AB-123-CD;1 234,50;Paul" & vbLf)
            Dim r = CsvSourceReader.ReadBatch(Options(dir), "{}", 100)
            Dim p = Assert.Single(r.Pesees)
            Assert.Equal("T1", p.SourceId)
            Assert.Equal(New DateTime(2026, 3, 1, 8, 15, 0), p.WeighedAt)
            Assert.Equal(1234.5D, p.NetKg)
            Assert.Equal("Paul", p.Extra("Chauffeur"))
        Finally
            Directory.Delete(dir, True)
        End Try
    End Sub

    <Fact>
    Public Sub ReadBatch_reprend_apres_le_curseur_et_respecte_la_taille_de_lot()
        Dim dir = NewFolder()
        Try
            Dim f = Path.Combine(dir, "a.csv")
            File.WriteAllText(f, "Ticket;Date;Immat;Net" & vbLf &
                "T1;01/03/2026 08:00:00;A;1" & vbLf & "T2;01/03/2026 09:00:00;B;2" & vbLf & "T3;01/03/2026 10:00:00;C;3" & vbLf)
            Dim o = Options(dir)

            Dim r1 = CsvSourceReader.ReadBatch(o, "0", 2)
            Assert.Equal({"T1", "T2"}, r1.Pesees.Select(Function(p) p.SourceId).ToArray())

            Dim r2 = CsvSourceReader.ReadBatch(o, r1.NewWatermark, 2)
            Assert.Equal({"T3"}, r2.Pesees.Select(Function(p) p.SourceId).ToArray())

            ' Ajout en fin de fichier : seule la nouvelle ligne repart.
            File.AppendAllText(f, "T4;01/03/2026 11:00:00;D;4" & vbLf)
            Dim r3 = CsvSourceReader.ReadBatch(o, r2.NewWatermark, 2)
            Assert.Equal({"T4"}, r3.Pesees.Select(Function(p) p.SourceId).ToArray())

            ' Plus rien de nouveau.
            Assert.Empty(CsvSourceReader.ReadBatch(o, r3.NewWatermark, 2).Pesees)
        Finally
            Directory.Delete(dir, True)
        End Try
    End Sub

    <Fact>
    Public Sub ReadBatch_fichier_remplace_est_relu_depuis_le_debut()
        Dim dir = NewFolder()
        Try
            Dim f = Path.Combine(dir, "export.csv")
            File.WriteAllText(f, "Ticket;Date;Immat;Net" & vbLf & "T1;01/03/2026 08:00:00;A;1" & vbLf)
            Dim o = Options(dir)
            Dim r1 = CsvSourceReader.ReadBatch(o, "{}", 10)

            File.WriteAllText(f, "Ticket;Date;Immat;Net" & vbLf & "T9;02/03/2026 08:00:00;Z;9" & vbLf & "T10;02/03/2026 09:00:00;Y;1" & vbLf)
            Dim r2 = CsvSourceReader.ReadBatch(o, r1.NewWatermark, 10)
            Assert.Equal(2, r2.Pesees.Count)
        Finally
            Directory.Delete(dir, True)
        End Try
    End Sub

    <Fact>
    Public Sub ReadBatch_colonne_inconnue_leve_une_erreur_claire()
        Dim dir = NewFolder()
        Try
            File.WriteAllText(Path.Combine(dir, "a.csv"), "X;Y" & vbLf & "1;2" & vbLf)
            Assert.Throws(Of InvalidOperationException)(Function() CsvSourceReader.ReadBatch(Options(dir), "{}", 10))
        Finally
            Directory.Delete(dir, True)
        End Try
    End Sub
End Class
