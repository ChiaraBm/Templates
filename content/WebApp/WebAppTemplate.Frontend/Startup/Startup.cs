using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace WebAppTemplate.Frontend.Startup;

public static partial class Startup
{
    public static void AddWebAppTemplate(this WebAssemblyHostBuilder builder)
    {
        builder.AddBase();
        builder.AddLogging();
        builder.AddAuthentication();
    }
}