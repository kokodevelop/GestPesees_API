Imports Microsoft.AspNetCore.Builder
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports GestPesees.Api.Data
Imports GestPesees.Api.Security

Public Module Program
    Public Sub Main(args As String())
        Dim builder = WebApplication.CreateBuilder(args)
        builder.Services.AddControllers()
        builder.Services.AddSingleton(Of ISiteDirectory, SqlSiteDirectory)()
        builder.Services.AddSingleton(Of IPeseeStore, SqlPeseeStore)()

        Dim app = builder.Build()
        app.UseHttpsRedirection()
        app.UseMiddleware(Of ApiKeyMiddleware)()
        app.MapControllers()
        app.Run()
    End Sub
End Module
