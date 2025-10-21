using System.Text.Json;
using MoonCore.Logging;

namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private static void AddLogging(this WebApplicationBuilder builder)
    {
        // Configure application logging
        builder.Logging.ClearProviders();
        builder.Logging.AddAnsiConsole();

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
            File.WriteAllText(logConfigPath, defaultJson);
        }

        // Configure logging configuration
        var logJson = File.ReadAllText(logConfigPath);
        var logLevels = JsonSerializer.Deserialize<Dictionary<string, string>>(logJson)!;

        foreach (var logLevel in logLevels)
        {
            var level = Enum.Parse<LogLevel>(logLevel.Value);
            builder.Logging.AddFilter(logLevel.Key, level);
        }

        // Mute exception handler middleware
        // https://github.com/dotnet/aspnetcore/issues/19740
        builder.Logging.AddFilter(
            "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
            LogLevel.Critical
        );

        builder.Logging.AddFilter(
            "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware",
            LogLevel.Critical
        );
    }
}