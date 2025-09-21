using MoonCore.Logging;

namespace WebAppTemplate.Frontend.Startup;

public partial class Startup
{
    private Task RegisterLoggingAsync()
    {
        WebAssemblyHostBuilder.Logging.ClearProviders();
        WebAssemblyHostBuilder.Logging.AddAnsiConsole();

        return Task.CompletedTask;
    }
}