using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MoonCore.EnvConfiguration;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Extensions;
using MoonCore.Extended.JwtInvalidation;
using MoonCore.Extensions;
using MoonCore.Logging;
using MoonCore.Yaml;
using WebAppTemplate.ApiServer.Configuration;
using WebAppTemplate.ApiServer.Database;
using WebAppTemplate.ApiServer.Implementations;

namespace WebAppTemplate.ApiServer;

public class Startup
{
    private string[] Args;

    // Configuration
    private AppConfiguration Configuration;

    // Logging
    private ILoggerFactory StartupLoggerFactory;
    private ILogger<Startup> StartupLogger;

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
        await RegisterAuth();

        await BuildWebApplication();
        
        await PrepareDatabase();

        await UseBase();
        await UseAuth();
        await UseBaseMiddleware();

        await MapBase();

        await WebApplication.RunAsync();
    }

    private Task SetupStorage()
    {
        Directory.CreateDirectory("storage");
        Directory.CreateDirectory(Path.Combine("storage", "logs"));

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

    #endregion

    #region Configurations

    private async Task SetupAppConfiguration()
    {
        // Configure configuration (wow)
        var configurationBuilder = new ConfigurationBuilder();
        
        // Ensure configuration file exists
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "storage", "config.yml");

        await YamlDefaultGenerator.Generate<AppConfiguration>(configPath);

        configurationBuilder.AddYamlFile(configPath);
        configurationBuilder.AddEnvironmentVariables(prefix: "WebAppTemplate_".ToUpper(), separator: "_");

        var configurationRoot = configurationBuilder.Build();

        // Retrieve configuration
        Configuration = configurationRoot.Get<AppConfiguration>()!;
    }

    private Task RegisterAppConfiguration()
    {
        WebApplicationBuilder.Services.AddSingleton(Configuration);
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
        StartupLoggerFactory = new LoggerFactory();
        
        StartupLoggerFactory.AddAnsiConsole();

        StartupLogger = StartupLoggerFactory.CreateLogger<Startup>();

        return Task.CompletedTask;
    }

    private async Task RegisterLogging()
    {
        // Configure application logging
        WebApplicationBuilder.Logging.ClearProviders();
        WebApplicationBuilder.Logging.AddAnsiConsole();
        WebApplicationBuilder.Logging.AddFile(Path.Combine("storage", "WebAppTemplate.log"));

        // Logging levels
        var logConfigPath = Path.Combine("storage", "logConfig.json");

        // Ensure logging config, add a default one is missing
        if (!File.Exists(logConfigPath))
        {
            var defaultLogLevels = new Dictionary<string, string>
            {
                { "Default", "Information" },
                { "Microsoft.AspNetCore", "Warning" },
                { "System.Net.Http.HttpClient", "Warning" }
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

    #endregion

    #region Database

    private Task RegisterDatabase()
    {
        WebApplicationBuilder.Services.AddDatabaseMappings();
        WebApplicationBuilder.Services.AddServiceCollectionAccessor();

        WebApplicationBuilder.Services.AddDbContext<DataContext>();
        
        WebApplicationBuilder.Services.AddScoped(typeof(DatabaseRepository<>));

        return Task.CompletedTask;
    }

    private async Task PrepareDatabase()
    {
        await WebApplication.Services.EnsureDatabaseMigrated();
        
        WebApplication.Services.GenerateDatabaseMappings();
    }

    #endregion

    #region Authentication & Authorisation

    private Task RegisterAuth()
    {
        WebApplicationBuilder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new()
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        Configuration.Authentication.Secret
                    )),
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateAudience = true,
                    ValidAudience = Configuration.PublicUrl,
                    ValidateIssuer = true,
                    ValidIssuer = Configuration.PublicUrl
                };
            });

        WebApplicationBuilder.Services.AddJwtBearerInvalidation();
        WebApplicationBuilder.Services.AddScoped<IJwtInvalidateHandler, UserJwtInvalidator>();

        WebApplicationBuilder.Services.AddAuthorization();
        
        return Task.CompletedTask;
    }

    private Task UseAuth()
    {
        WebApplication.UseAuthentication();
        WebApplication.UseAuthorization();
        
        return Task.CompletedTask;
    }

    #endregion
}