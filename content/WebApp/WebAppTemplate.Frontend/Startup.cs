using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MoonCore.Blazor.Services;
using MoonCore.Blazor.Tailwind.Auth;
using MoonCore.Blazor.Tailwind.Extensions;
using MoonCore.Extensions;
using MoonCore.Helpers;
using WebAppTemplate.Frontend.Services;
using WebAppTemplate.Frontend.UI;

namespace WebAppTemplate.Frontend;

public class Startup
{
    private string[] Args;

    // Logging
    private ILoggerProvider[] LoggerProviders;
    private ILoggerFactory LoggerFactory;
    private ILogger<Startup> Logger;

    // WebAssemblyHost
    private WebAssemblyHostBuilder WebAssemblyHostBuilder;
    private WebAssemblyHost WebAssemblyHost;

    public async Task Run(string[] args)
    {
        Args = args;

        await SetupLogging();

        await CreateWebAssemblyHostBuilder();

        await RegisterLogging();
        await RegisterBase();
        await RegisterAuthentication();

        await BuildWebAssemblyHost();

        await WebAssemblyHost.RunAsync();
    }

    private Task RegisterBase()
    {
        WebAssemblyHostBuilder.RootComponents.Add<App>("#app");
        WebAssemblyHostBuilder.RootComponents.Add<HeadOutlet>("head::after");

        WebAssemblyHostBuilder.Services.AddScoped(_ =>
            new HttpClient
            {
                BaseAddress = new Uri(WebAssemblyHostBuilder.HostEnvironment.BaseAddress)
            }
        );

        WebAssemblyHostBuilder.Services.AddScoped(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();
            var httpApiClient = new HttpApiClient(httpClient);

            var localStorageService = sp.GetRequiredService<LocalStorageService>();

            httpApiClient.OnConfigureRequest += async request =>
            {
                var accessToken = await localStorageService.GetString("AccessToken");

                if (string.IsNullOrEmpty(accessToken))
                    return;

                request.Headers.Add("Authorization", $"Bearer {accessToken}");
            };

            return httpApiClient;
        });
        
        WebAssemblyHostBuilder.Services.AddMoonCoreBlazorTailwind();
        WebAssemblyHostBuilder.Services.AddScoped<LocalStorageService>();

        WebAssemblyHostBuilder.Services.AutoAddServices<Program>();

        return Task.CompletedTask;
    }
    
    private Task RegisterAuthentication()
    {
        WebAssemblyHostBuilder.Services.AddAuthorizationCore();
        WebAssemblyHostBuilder.Services.AddCascadingAuthenticationState();
        
        WebAssemblyHostBuilder.Services.AddAuthenticationStateManager<RemoteAuthStateManager>();
        
        return Task.CompletedTask;
    }

    #region Logging

    private Task SetupLogging()
    {
        LoggerProviders = LoggerBuildHelper.BuildFromConfiguration(configuration =>
        {
            configuration.Console.Enable = true;
            configuration.Console.EnableAnsiMode = true;
            configuration.FileLogging.Enable = false;
        });

        LoggerFactory = new LoggerFactory();
        LoggerFactory.AddProviders(LoggerProviders);

        Logger = LoggerFactory.CreateLogger<Startup>();

        return Task.CompletedTask;
    }

    private Task RegisterLogging()
    {
        WebAssemblyHostBuilder.Logging.ClearProviders();
        WebAssemblyHostBuilder.Logging.AddProviders(LoggerProviders);

        return Task.CompletedTask;
    }

    #endregion

    #region Web Application

    private Task CreateWebAssemblyHostBuilder()
    {
        WebAssemblyHostBuilder = WebAssemblyHostBuilder.CreateDefault(Args);
        return Task.CompletedTask;
    }

    private Task BuildWebAssemblyHost()
    {
        WebAssemblyHost = WebAssemblyHostBuilder.Build();
        return Task.CompletedTask;
    }

    #endregion
}