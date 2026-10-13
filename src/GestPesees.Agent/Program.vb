Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting

Public Module Program
    Public Sub Main(args As String())
        ' Configuration lue à côté de l'exécutable, quel que soit le répertoire courant
        ' (service Windows, ou "dotnet run --project ..." lancé depuis la racine du dépôt).
        Dim builder = Host.CreateApplicationBuilder(New HostApplicationBuilderSettings With {
            .Args = args,
            .ContentRootPath = AppContext.BaseDirectory
        })
        builder.Services.AddWindowsService(Sub(o) o.ServiceName = "GestPesees Agent")
        builder.Services.Configure(Of AgentOptions)(builder.Configuration.GetSection("Agent"))
        builder.Services.AddHostedService(Of Worker)()
        builder.Build().Run()
    End Sub
End Module
