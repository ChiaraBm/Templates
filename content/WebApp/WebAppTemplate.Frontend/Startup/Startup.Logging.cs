using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MoonCore.Logging;

namespace WebAppTemplate.Frontend.Startup;

public partial class Startup
{
    private static void AddLogging(this WebAssemblyHostBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddAnsiConsole();
    }
}