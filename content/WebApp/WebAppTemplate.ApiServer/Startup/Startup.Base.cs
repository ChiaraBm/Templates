using MoonCore.Extended.Extensions;
using MoonCore.Extensions;

namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private Task RegisterBaseAsync()
    {
        WebApplicationBuilder.Services.AutoAddServices<Startup>();
        WebApplicationBuilder.Services.AddControllers();

        WebApplicationBuilder.Services.AddApiExceptionHandler();

        return Task.CompletedTask;
    }
    
    private Task UseBaseAsync()
    {
        WebApplication.UseRouting();
        WebApplication.UseExceptionHandler();

        WebApplication.UseBlazorFrameworkFiles();
        WebApplication.UseStaticFiles();

        return Task.CompletedTask;
    }
    
    private Task UseBaseMiddlewareAsync()
    {
        return Task.CompletedTask;
    }

    private Task MapBaseAsync()
    {
        WebApplication.MapControllers();
        WebApplication.MapFallbackToFile("index.html");

        return Task.CompletedTask;
    }
}