Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting

Public Module Program
    Public Sub Main(args As String())
        Dim builder = Host.CreateApplicationBuilder(args)
        builder.Services.AddWindowsService(Sub(o) o.ServiceName = "GestPesees Agent")
        builder.Services.Configure(Of AgentOptions)(builder.Configuration.GetSection("Agent"))
        builder.Services.AddHostedService(Of Worker)()
        builder.Build().Run()
    End Sub
End Module
