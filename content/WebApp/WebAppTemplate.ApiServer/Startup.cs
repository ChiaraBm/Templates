using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MoonCore.Configuration;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Extensions;
using MoonCore.Extended.Helpers;
using MoonCore.Extended.JwtInvalidation;
using MoonCore.Extensions;
using MoonCore.Helpers;
using MoonCore.Services;
using WebAppTemplate.ApiServer.Configuration;
using WebAppTemplate.ApiServer.Database;
using WebAppTemplate.ApiServer.Database.Entities;

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
        WebApplication.UseApiExceptionHandler();

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
        WebApplicationBuilder.Services.AddDatabaseMappings();

        WebApplicationBuilder.Services.AddDbContext<DbContext, DataContext>();
        
        WebApplicationBuilder.Services.AddScoped(typeof(DatabaseRepository<>));
        WebApplicationBuilder.Services.AddScoped(typeof(CrudHelper<,>));

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
        
        WebApplicationBuilder.Services.AddJwtInvalidation(options =>
        {
            options.InvalidateTimeProvider = async (provider, principal) =>
            {
                var userIdClaim = principal.Claims.First(x => x.Type == "userId");
                var userId = int.Parse(userIdClaim.Value);
                
                var userRepository = provider.GetRequiredService<DatabaseRepository<User>>();
                var user = await userRepository.Get().FirstOrDefaultAsync(x => x.Id == userId);
                
                // If no user has been found with that id, we invalidate the jwt by setting the invalidation date to max
                if(user == null)
                    return DateTime.MaxValue;

                return user.InvalidateTimestamp;
            };
        });

        WebApplicationBuilder.Services.AddAuthorization();
        
        return Task.CompletedTask;
    }

    private Task UseAuth()
    {
        WebApplication.UseAuthentication();
        
        WebApplication.UseJwtInvalidation();
        
        WebApplication.UseAuthorization();
        
        return Task.CompletedTask;
    }

    #endregion
}