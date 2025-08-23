using Microsoft.AspNetCore.Components.Web;
using MoonCore.Blazor.FlyonUi;
using MoonCore.Blazor.Services;
using MoonCore.Extensions;
using MoonCore.Helpers;
using WebAppTemplate.Frontend.UI;

namespace WebAppTemplate.Frontend.Startup;

public partial class Startup
{
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
            return new HttpApiClient(httpClient);
        });
        
        WebAssemblyHostBuilder.Services.AddFlyonUiServices();
        WebAssemblyHostBuilder.Services.AddScoped<LocalStorageService>();

        WebAssemblyHostBuilder.Services.AutoAddServices<Startup>();

        return Task.CompletedTask;
    }
}