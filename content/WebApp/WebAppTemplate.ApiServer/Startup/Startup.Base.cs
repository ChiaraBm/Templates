using MoonCore.Extended.Extensions;
using MoonCore.Extensions;

namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private static void AddBase(this WebApplicationBuilder builder)
    {
        builder.Services.AutoAddServices<IAssemblyMarker>();
        builder.Services.AddControllers();

        builder.Services.AddApiExceptionHandler();
    }
    
    private static void UseBase(this WebApplication app)
    {
        app.UseRouting();
        app.UseExceptionHandler();

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
    }

    private static void MapBase(this WebApplication app)
    {
        app.MapControllers();
        app.MapFallbackToFile("index.html");
    }
}