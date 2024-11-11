using System.Text.Json;
using MoonCore.Authentication;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Extensions;
using MoonCore.Extended.Helpers;
using MoonCore.Extended.Models;
using MoonCore.Extended.OAuth2.Consumer;
using MoonCore.Extensions;
using MoonCore.Helpers;
using WebApp.ApiServer.Configuration;
using WebApp.ApiServer.Database;
using WebApp.ApiServer.Database.Entities;

namespace WebApp.ApiServer;

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
            configuration.FileLogging.RotateLogNameTemplate = PathBuilder.File("storage", "logs", "moonlight.log.{0}");
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
    
    #region Token Authentication

    public static Task ConfigureTokenAuthentication(WebApplicationBuilder builder, AppConfiguration config)
    {
        builder.AddTokenAuthentication(authenticationConfig =>
        {
            authenticationConfig.AccessSecret = config.Authentication.AccessSecret;
            authenticationConfig.RefreshSecret = config.Authentication.RefreshSecret;

            authenticationConfig.AccessDuration = config.Authentication.AccessDuration;
            authenticationConfig.RefreshDuration = config.Authentication.RefreshDuration;

            authenticationConfig.ProcessAccess = (accessData, provider, httpContext) =>
            {
                if (!accessData.TryGetValue("userId", out var userIdStr) || !userIdStr.TryGetInt32(out var userId))
                    return Task.FromResult(false);

                var userRepo = provider.GetRequiredService<DatabaseRepository<User>>();
                var user = userRepo.Get().FirstOrDefault(x => x.Id == userId);

                if (user == null)
                    return Task.FromResult(false);

                // Save permission state
                httpContext.User = new PermClaimsPrinciple([])
                {
                    IdentityModel = user
                };

                return Task.FromResult(true);
            };

            authenticationConfig.ProcessRefresh = async (oldData, newData, serviceProvider) =>
            {
                // Check if the userId is present in the refresh token
                if (!oldData.TryGetValue("userId", out var userIdStr) ||
                    !userIdStr.TryGetInt32(out var userId))
                {
                    return false;
                }

                // Load user from database if existent
                var userRepo = serviceProvider.GetRequiredService<DatabaseRepository<User>>();

                var user = userRepo
                    .Get()
                    .FirstOrDefault(x => x.Id == userId);

                if (user == null)
                    return false;

                // Check if it's time to resync with the oauth2 provider
                if (DateTime.UtcNow >= user.RefreshTimestamp)
                {
                    var oAuth2Service = serviceProvider.GetRequiredService<OAuth2ConsumerService>();

                    try
                    {
                        // It's time to refresh the access to the external oauth2 provider
                        var refreshData = oAuth2Service.RefreshAccess(user.RefreshToken).Result;

                        // Sync user with oauth2 provider
                        var syncedUser = await Sync(serviceProvider, refreshData.AccessToken);

                        if (syncedUser == null) // User sync has failed. No refresh allowed
                            return false;

                        // Save oauth2 refresh and access tokens for later use (re-authentication etc.).
                        // Fetch user model in current db context, just in case the oauth2 provider
                        // uses a different db context or smth

                        var userModel = userRepo
                            .Get()
                            .First(x => x.Id == syncedUser.Id);

                        userModel.AccessToken = refreshData.AccessToken;
                        userModel.RefreshToken = refreshData.RefreshToken;
                        userModel.RefreshTimestamp = DateTime.UtcNow.AddSeconds(refreshData.ExpiresIn);

                        userRepo.Update(userModel);
                    }
                    catch (Exception e)
                    {
                        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger("OAuth2 Refresh");

                        // We are handling this error more softly, because it will occur when a user hasn't logged in a long period of time
                        logger.LogTrace("An error occured while refreshing external oauth2 access: {e}", e);
                        return false;
                    }
                }

                // All checks have passed, allow refresh
                newData.Add("userId", user.Id);
                return true;
            };
        });

        return Task.CompletedTask;
    }

    public static Task UseTokenAuthentication(WebApplication builder)
    {
        builder.UseTokenAuthentication();

        return Task.CompletedTask;
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
        if (config.Authentication.UseLocalOAuth2)
        {
            logger.LogInformation("Using local oauth2 provider");
        
            builder.AddOAuth2ConsumerWithLocalProvider(configuration =>
            {
                configuration.AccessSecret = config.Authentication.LocalOAuth2.AccessSecret;
                configuration.RefreshSecret = config.Authentication.LocalOAuth2.RefreshSecret;

                configuration.ClientId = config.Authentication.OAuth2.ClientId;
                configuration.ClientSecret = config.Authentication.OAuth2.ClientSecret;
                configuration.CodeSecret = config.Authentication.LocalOAuth2.CodeSecret;
                configuration.AuthorizationRedirect =
                    config.Authentication.OAuth2.AuthorizationRedirect ?? $"{config.PublicUrl}/";
                configuration.AccessTokenDuration = 60;
                configuration.RefreshTokenDuration = 3600;
            }, config.PublicUrl);
        }
        else
        {
            throw new NotImplementedException();
            
            builder.AddOAuth2Consumer(configuration =>
            {
                configuration.ClientId = config.Authentication.OAuth2.ClientId;
                configuration.ClientSecret = config.Authentication.OAuth2.ClientSecret;
                configuration.AuthorizationRedirect =
                    config.Authentication.OAuth2.AuthorizationRedirect ?? $"{config.PublicUrl}/";

                configuration.AccessEndpoint =
                    config.Authentication.OAuth2.AccessEndpoint ?? $"{config.PublicUrl}/oauth2/access";
                configuration.RefreshEndpoint =
                    config.Authentication.OAuth2.RefreshEndpoint ?? $"{config.PublicUrl}/oauth2/refresh";
                
                configuration.AuthorizationEndpoint = config.Authentication.OAuth2.AuthorizationUri!;
            });
        }

        return Task.CompletedTask;
    }

    public static Task UseOAuth2(WebApplication application)
    {
        application.UseOAuth2Consumer();
        application.UseOAuth2LocalProvider(configuration =>
        {
            configuration.Validate = (provider, userId) =>
            {
                var userRepo = provider.GetRequiredService<DatabaseRepository<User>>();
                var user = userRepo.Get().FirstOrDefault(x => x.Id == userId);

                if (user == null)
                    return Task.FromResult(false);

                return Task.FromResult(true);
            };

            configuration.LoadUserData = (provider, userId) =>
            {
                var userRepo = provider.GetRequiredService<DatabaseRepository<User>>();
                var user = userRepo.Get().First(x => x.Id == userId);

                return Task.FromResult<LocalOAuth2User>(new()
                {
                    Username = user.Username,
                    Email = user.Email
                });
            };

            configuration.HandleLogin += (provider, email, password) =>
            {
                var userRepo = provider.GetRequiredService<DatabaseRepository<User>>();
                var user = userRepo.Get().FirstOrDefault(x => x.Email == email);

                if (user == null)
                    throw new HttpApiException("Invalid username or password", 400);

                if (!HashHelper.Verify(password, user.Password))
                    throw new HttpApiException("Invalid username or password", 400);

                return Task.FromResult(user.Id);
            };

            configuration.HandleRegister += (provider, username, email, password) =>
            {
                var userRepo = provider.GetRequiredService<DatabaseRepository<User>>();

                if (userRepo.Get().Any(x => x.Username == username))
                    throw new HttpApiException("A user with that username does already exist", 400);
                
                if (userRepo.Get().Any(x => x.Email == email))
                    throw new HttpApiException("A user with that email does already exist", 400);

                var user = new User()
                {
                    Email = email,
                    Username = username,
                    Password = HashHelper.Hash(password)
                };

                var finalUser = userRepo.Add(user);

                return Task.FromResult(finalUser.Id);
            };

            configuration.AllowRegister = true;
        });

        return Task.CompletedTask;
    }

    #endregion
}