using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MoonCore.Blazor.Extensions;
using MoonCore.Blazor.Services;
using MoonCore.Blazor.Tailwind.Extensions;
using MoonCore.Blazor.Tailwind.Forms;
using MoonCore.Blazor.Tailwind.Forms.Components;
using MoonCore.Exceptions;
using MoonCore.Extensions;
using MoonCore.Helpers;
using MoonCore.Models;
using WebApp.Frontend.UI;
using WebApp.Shared.Http.Requests.Auth;
using WebApp.Shared.Http.Responses;

// Build pre run logger
var providers = LoggerBuildHelper.BuildFromConfiguration(configuration =>
{
    configuration.Console.Enable = true;
    configuration.Console.EnableAnsiMode = true;
    configuration.FileLogging.Enable = false;
});

using var loggerFactory = new LoggerFactory(providers);
var logger = loggerFactory.CreateLogger("Startup");

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure application logging
builder.Logging.ClearProviders();
builder.Logging.AddProviders(providers);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var localStorageService = sp.GetRequiredService<LocalStorageService>();
    var result = new HttpApiClient(httpClient);
    
    result.AddLocalStorageTokenAuthentication(localStorageService, async refreshToken =>
    {
        try
        {
            var httpApiClient = new HttpApiClient(httpClient);

            var response = await httpApiClient.PostJson<RefreshResponse>(
                "api/auth/refresh",
                new RefreshRequest()
                {
                    RefreshToken = refreshToken
                }
            );

            return (new TokenPair()
            {
                AccessToken = response.AccessToken,
                RefreshToken = response.RefreshToken
            }, response.ExpiresAt);
        }
        catch (HttpApiException)
        {
            return (new TokenPair()
            {
                AccessToken = "unset",
                RefreshToken = "unset"
            }, DateTime.MinValue);
        }
    });

    return result;
});

builder.Services.AddMoonCoreBlazorTailwind();
builder.Services.AddScoped<LocalStorageService>();

builder.Services.AutoAddServices<Program>();

FormComponentRepository.Set<string, StringComponent>();
FormComponentRepository.Set<int, IntComponent>();

var app = builder.Build();

await app.RunAsync();