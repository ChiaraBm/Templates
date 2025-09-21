using System.Text.Json;
using MoonCore.Logging;

namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private async Task RegisterLoggingAsync()
    {
        // Configure application logging
        WebApplicationBuilder.Logging.ClearProviders();
        WebApplicationBuilder.Logging.AddAnsiConsole();

        // Logging levels
        var logConfigPath = Path.Combine("storage", "logConfig.json");

        // Ensure logging config, add a default one is missing
        if (!File.Exists(logConfigPath))
        {
            var defaultLogLevels = new Dictionary<string, string>
            {
                { "Default", "Information" },
                { "Microsoft.AspNetCore", "Warning" },
                { "System.Net.Http.HttpClient", "Warning" },
                { "WebAppTemplate.ApiServer.Implementations.LocalAuth.LocalAuthHandler", "Warning" }
            };

            var defaultJson = JsonSerializer.Serialize(defaultLogLevels);
            await File.WriteAllTextAsync(logConfigPath, defaultJson);
        }

        // Configure logging configuration
        var logJson = await File.ReadAllTextAsync(logConfigPath);
        var logLevels = JsonSerializer.Deserialize<Dictionary<string, string>>(logJson)!;

        foreach (var logLevel in logLevels)
        {
            var level = Enum.Parse<LogLevel>(logLevel.Value);
            WebApplicationBuilder.Logging.AddFilter(logLevel.Key, level);
        }

        // Mute exception handler middleware
        // https://github.com/dotnet/aspnetcore/issues/19740
        WebApplicationBuilder.Logging.AddFilter(
            "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
            LogLevel.Critical
        );

        WebApplicationBuilder.Logging.AddFilter(
            "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware",
            LogLevel.Critical
        );
    }
}