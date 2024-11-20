using System.Text.Json;
using MoonCore.Configuration;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Extensions;
using MoonCore.Extended.Helpers;
using MoonCore.Extended.OAuth2.Consumer;
using MoonCore.Extended.OAuth2.Consumer.Extensions;
using MoonCore.Extended.OAuth2.LocalProvider;
using MoonCore.Extended.OAuth2.LocalProvider.Extensions;
using MoonCore.Extended.OAuth2.LocalProvider.Implementations;
using MoonCore.Extensions;
using MoonCore.Helpers;
using MoonCore.Services;
using WebAppTemplate.ApiServer.Configuration;
using WebAppTemplate.ApiServer.Database;
using WebAppTemplate.ApiServer.Database.Entities;
using WebAppTemplate.ApiServer.Http.Middleware;
using WebAppTemplate.ApiServer.Implementations.OAuth2;

namespace WebAppTemplate.ApiServer;

public class Startup
{
    private string[] Args;

    // Configuration
    private AppConfiguration Configuration;
    private ConfigurationService ConfigurationService;
    private ConfigurationOptions ConfigurationOptions;

    // Logging
    private ILoggerProvider[] LoggerProviders;
    private ILoggerFactory LoggerFactory;
    private ILogger<Startup> Logger;

    // WebApplication Stuff
    private WebApplication WebApplication;
    private WebApplicationBuilder WebApplicationBuilder;

    public async Task Run(string[] args)
    {
        Args = args;

        await SetupStorage();
        await SetupAppConfiguration();
        await SetupLogging();

        await CreateWebApplicationBuilder();

        await RegisterAppConfiguration();
        await RegisterLogging();
        await RegisterBase();
        await RegisterDatabase();
        await RegisterOAuth2();

        await BuildWebApplication();

        await PrepareDatabase();

        await UseBase();
        await UseOAuth2();
        await UseBaseMiddleware();

        await MapBase();
        await MapOAuth2();

        await WebApplication.RunAsync();
    }

    private Task SetupStorage()
    {
        Directory.CreateDirectory("storage");
        Directory.CreateDirectory(PathBuilder.Dir("storage", "logs"));
        Directory.CreateDirectory(PathBuilder.Dir("storage", "plugins"));

        return Task.CompletedTask;
    }

    #region Base

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

        WebApplication.UseBlazorFrameworkFiles();
        WebApplication.UseStaticFiles();

        return Task.CompletedTask;
    }

    private Task UseBaseMiddleware()
    {
        WebApplication.UseMiddleware<AuthorizationMiddleware>();

        return Task.CompletedTask;
    }

    private Task MapBase()
    {
        WebApplication.MapControllers();
        WebApplication.MapFallbackToFile("index.html");

        return Task.CompletedTask;
    }

    #endregion

    #region Configurations

    private Task SetupAppConfiguration()
    {
        ConfigurationService = new ConfigurationService();

        // Setup options
        ConfigurationOptions = new ConfigurationOptions();

        ConfigurationOptions.AddConfiguration<AppConfiguration>("app");
        ConfigurationOptions.Path = PathBuilder.Dir("storage");
        ConfigurationOptions.EnvironmentPrefix = "WebAppTemplate".ToUpper();

        // Create minimal logger
        var loggerFactory = new LoggerFactory();

        loggerFactory.AddMoonCore(configuration =>
        {
            configuration.Console.Enable = true;
            configuration.Console.EnableAnsiMode = true;
            configuration.FileLogging.Enable = false;
        });

        var logger = loggerFactory.CreateLogger<ConfigurationService>();

        // Retrieve configuration
        Configuration = ConfigurationService.GetConfiguration<AppConfiguration>(
            ConfigurationOptions,
            logger
        );

        return Task.CompletedTask;
    }

    private Task RegisterAppConfiguration()
    {
        ConfigurationService.RegisterInDi(ConfigurationOptions, WebApplicationBuilder.Services);
        WebApplicationBuilder.Services.AddSingleton(ConfigurationService);

        return Task.CompletedTask;
    }

    #endregion

    #region Web Application

    private Task CreateWebApplicationBuilder()
    {
        WebApplicationBuilder = WebApplication.CreateBuilder(Args);
        return Task.CompletedTask;
    }

    private Task BuildWebApplication()
    {
        WebApplication = WebApplicationBuilder.Build();
        return Task.CompletedTask;
    }

    #endregion

    #region Logging

    private Task SetupLogging()
    {
        LoggerProviders = LoggerBuildHelper.BuildFromConfiguration(configuration =>
        {
            configuration.Console.Enable = true;
            configuration.Console.EnableAnsiMode = true;

            configuration.FileLogging.Enable = true;
            configuration.FileLogging.EnableLogRotation = true;
            configuration.FileLogging.Path = PathBuilder.File("storage", "logs", "WebAppTemplate.log");
            configuration.FileLogging.RotateLogNameTemplate =
                PathBuilder.File("storage", "logs", "WebAppTemplate.log.{0}");
        });

        LoggerFactory = new LoggerFactory();
        LoggerFactory.AddProviders(LoggerProviders);

        Logger = LoggerFactory.CreateLogger<Startup>();

        return Task.CompletedTask;
    }

    private async Task RegisterLogging()
    {
        // Configure application logging
        WebApplicationBuilder.Logging.ClearProviders();
        WebApplicationBuilder.Logging.AddProviders(LoggerProviders);

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

        // Configure logging configuration
        WebApplicationBuilder.Logging.AddConfiguration(
            await File.ReadAllTextAsync(logConfigPath)
        );
        
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

    #endregion

    #region Database

    private Task RegisterDatabase()
    {
        var logger = LoggerFactory.CreateLogger<DatabaseHelper>();
        var databaseHelper = new DatabaseHelper(logger);

        // Add databases here
        databaseHelper.AddDbContext<DataContext>();
        WebApplicationBuilder.Services.AddScoped<DataContext>();

        databaseHelper.GenerateMappings();

        // Register services
        WebApplicationBuilder.Services.AddSingleton(databaseHelper);
        WebApplicationBuilder.Services.AddScoped(typeof(DatabaseRepository<>));
        WebApplicationBuilder.Services.AddScoped(typeof(CrudHelper<,>));

        return Task.CompletedTask;
    }

    private async Task PrepareDatabase()
    {
        using var scope = WebApplication.Services.CreateScope();
        var databaseHelper = scope.ServiceProvider.GetRequiredService<DatabaseHelper>();

        await databaseHelper.EnsureMigrated(scope.ServiceProvider);
    }

    #endregion

    #region OAuth2

    private Task RegisterOAuth2()
    {
        WebApplicationBuilder.Services.AddOAuth2Authentication<User>(configuration =>
        {
            configuration.AccessSecret = Configuration.Authentication.AccessSecret;
            configuration.RefreshSecret = Configuration.Authentication.RefreshSecret;
            configuration.RefreshInterval = TimeSpan.FromSeconds(Configuration.Authentication.RefreshInterval);
            configuration.RefreshDuration = TimeSpan.FromSeconds(Configuration.Authentication.RefreshDuration);

            configuration.ClientId = Configuration.Authentication.ClientId;
            configuration.ClientSecret = Configuration.Authentication.ClientSecret;
            configuration.RedirectUri = Configuration.Authentication.RedirectUri ?? Configuration.PublicUrl;
            configuration.AuthorizeEndpoint = Configuration.Authentication.AuthorizeEndpoint ??
                                              Configuration.PublicUrl + "/api/_auth/oauth2/authorize";
        });

        WebApplicationBuilder.Services.AddScoped<IDataProvider<User>, LocalUserOAuth2Provider>();

        if (!Configuration.Authentication.UseLocalOAuth2)
            return Task.CompletedTask;

        WebApplicationBuilder.Services.AddLocalOAuth2Provider<User>(Configuration.PublicUrl);
        WebApplicationBuilder.Services.AddScoped<ILocalProviderImplementation<User>, LocalUserOAuth2Provider>();

        return Task.CompletedTask;
    }

    private Task UseOAuth2()
    {
        WebApplication.UseOAuth2Authentication<User>();
        return Task.CompletedTask;
    }

    private Task MapOAuth2()
    {
        WebApplication.MapOAuth2Authentication<User>();

        if (!Configuration.Authentication.UseLocalOAuth2)
            return Task.CompletedTask;
        
        WebApplication.MapLocalOAuth2Provider<User>();
        return Task.CompletedTask;
    }

    #endregion
}