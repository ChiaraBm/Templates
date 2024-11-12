using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MoonCore.Blazor.Extensions;
using MoonCore.Blazor.Services;
using MoonCore.Blazor.Tailwind.Extensions;
using MoonCore.Blazor.Tailwind.Forms;
using MoonCore.Blazor.Tailwind.Forms.Components;
using MoonCore.Extensions;
using MoonCore.Helpers;
using WebAppTemplate.Frontend.UI;

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

builder.AddTokenAuthentication();
builder.AddOAuth2();

builder.Services.AddMoonCoreBlazorTailwind();
builder.Services.AddScoped<LocalStorageService>();

builder.Services.AutoAddServices<Program>();

FormComponentRepository.Set<string, StringComponent>();
FormComponentRepository.Set<int, IntComponent>();

var app = builder.Build();

await app.RunAsync();