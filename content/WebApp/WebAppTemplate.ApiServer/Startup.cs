using System.Text.Json;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using MoonCore.Extended.OAuth2.Consumer;
using MoonCore.Extended.OAuth2.Consumer.Extensions;
using MoonCore.Extended.OAuth2.LocalProvider;
using MoonCore.Extended.OAuth2.LocalProvider.Extensions;
using MoonCore.Extended.OAuth2.LocalProvider.Implementations;
using MoonCore.Extensions;
using MoonCore.Helpers;
using WebAppTemplate.ApiServer.Configuration;
using WebAppTemplate.ApiServer.Database;
using WebAppTemplate.ApiServer.Database.Entities;
using WebAppTemplate.ApiServer.Implementations.OAuth2;

namespace WebAppTemplate.ApiServer;

public class Startup
{
    #region Logging

    public static async Task ConfigureLogging(IHostApplicationBuilder builder)
    {
        // Create logging path
        Directory.CreateDirectory(PathBuilder.Dir("storage", "logs"));

        // Configure application logging
        builder.Logging.ClearProviders();

        builder.Logging.AddMoonCore(configuration =>
        {
            configuration.Console.Enable = true;
            configuration.Console.EnableAnsiMode = true;

            configuration.FileLogging.Enable = true;
            configuration.FileLogging.Path = PathBuilder.File("storage", "logs", "WebApp.log");
            configuration.FileLogging.EnableLogRotation = true;
            configuration.FileLogging.RotateLogNameTemplate = PathBuilder.File("storage", "logs", "WebApp.log.{0}");
        });

        // Logging levels
        var logConfigPath = PathBuilder.File("storage", "logConfig.json");

        // Ensure logging config, add a default one is missing
        if (!File.Exists(logConfigPath))
        {
            var logLevels = new Dictionary<string, string>
            {
                { "Default", "Information" },
                { "Microsoft.AspNetCore", "Warning" },
                { "System.Net.Http.HttpClient", "Warning" }
            };

            var logLevelsJson = JsonSerializer.Serialize(logLevels);
            var logConfig = "{\"LogLevel\":" + logLevelsJson + "}";
            await File.WriteAllTextAsync(logConfigPath, logConfig);
        }

        builder.Logging.AddConfiguration(await File.ReadAllTextAsync(logConfigPath));
    }

    #endregion
    
    #region Database

    public static async Task ConfigureDatabase(IHostApplicationBuilder builder, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<DatabaseHelper>();
        var databaseHelper = new DatabaseHelper(logger);

        // Add databases here
        databaseHelper.AddDbContext<DataContext>();
        builder.Services.AddScoped<DataContext>();

        databaseHelper.GenerateMappings();

        builder.Services.AddSingleton(databaseHelper);
        builder.Services.AddScoped(typeof(DatabaseRepository<>));
        builder.Services.AddScoped(typeof(CrudHelper<,>));
    }

    public static async Task PrepareDatabase(IApplicationBuilder builder)
    {
        using var scope = builder.ApplicationServices.CreateScope();
        var databaseHelper = scope.ServiceProvider.GetRequiredService<DatabaseHelper>();

        await databaseHelper.EnsureMigrated(scope.ServiceProvider);
    }

    #endregion
    
    #region OAuth2

    public static Task ConfigureOAuth2(WebApplicationBuilder builder, ILogger logger, AppConfiguration config)
    {
        builder.AddOAuth2Authentication<User>(configuration =>
        {
            configuration.AccessSecret = config.Authentication.AccessSecret;
            configuration.RefreshSecret = config.Authentication.RefreshSecret;
            configuration.RefreshInterval = TimeSpan.FromSeconds(config.Authentication.RefreshInterval);
            configuration.RefreshDuration = TimeSpan.FromSeconds(config.Authentication.RefreshDuration);

            configuration.ClientId = config.Authentication.ClientId;
            configuration.ClientSecret = config.Authentication.ClientSecret;
            configuration.RedirectUri = config.Authentication.RedirectUri ?? config.PublicUrl;
            configuration.AuthorizeEndpoint = config.Authentication.AuthorizeEndpoint ?? config.PublicUrl + "/api/_auth/oauth2/authorize";
        });

        builder.Services.AddScoped<IDataProvider<User>, LocalUserOAuth2Provider>();

        if (!config.Authentication.UseLocalOAuth2)
            return Task.CompletedTask;
        
        builder.AddLocalOAuth2Provider<User>(config.PublicUrl);
        builder.Services.AddScoped<ILocalProviderImplementation<User>, LocalUserOAuth2Provider>();
        
        return Task.CompletedTask;
    }

    public static Task UseOAuth2(WebApplication application, AppConfiguration config)
    {
        application.UseOAuth2Authentication<User>();
        
        if(!config.Authentication.UseLocalOAuth2)
            return Task.CompletedTask;
        
        application.UseLocalOAuth2Provider<User>();

        return Task.CompletedTask;
    }

    #endregion
}