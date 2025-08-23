using MoonCore.Extended.Extensions;
using MoonCore.Extensions;

namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private Task RegisterBase()
    {
        WebApplicationBuilder.Services.AutoAddServices<Startup>();
        WebApplicationBuilder.Services.AddControllers();

        WebApplicationBuilder.Services.AddApiExceptionHandler();

        return Task.CompletedTask;
    }
    
    private Task UseBase()
    {
        WebApplication.UseRouting();
        WebApplication.UseExceptionHandler();

        WebApplication.UseBlazorFrameworkFiles();
        WebApplication.UseStaticFiles();

        return Task.CompletedTask;
    }
    
    private Task UseBaseMiddleware()
    {
        return Task.CompletedTask;
    }

    private Task MapBase()
    {
        WebApplication.MapControllers();
        WebApplication.MapFallbackToFile("index.html");

        return Task.CompletedTask;
    }
}