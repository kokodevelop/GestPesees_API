Imports System.Net
Imports System.ServiceProcess

Public Module Program
    Public Sub Main(args As String())
        ' Anciens postes : forcer TLS 1.2 (l'API est en HTTPS). Nécessite Windows 7 SP1 minimum.
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12

        If Environment.UserInteractive Then
            ' Lancé à la main (ou avec --console) : utile pour tester la configuration avant d'installer le service.
            Dim svc As New AgentService()
            svc.StartLoop()
            Console.WriteLine("Agent en cours. Appuyez sur Entrée pour arrêter.")
            Console.ReadLine()
            svc.StopLoop()
        Else
            ServiceBase.Run(New AgentService())
        End If
    End Sub
End Module
