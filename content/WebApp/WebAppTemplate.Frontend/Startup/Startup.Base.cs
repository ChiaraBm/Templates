using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MoonCore.Blazor.FlyonUi;
using MoonCore.Extensions;
using MoonCore.Helpers;
using WebAppTemplate.Frontend.UI;

namespace WebAppTemplate.Frontend.Startup;

public partial class Startup
{
    private static void AddBase(this WebAssemblyHostBuilder builder)
    {
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(_ =>
            new HttpClient
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
            }
        );

        builder.Services.AddScoped(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();
            return new HttpApiClient(httpClient);
        });
        
        builder.Services.AddFlyonUiServices();

        builder.Services.AutoAddServices<IAssemblyMarker>();
    }
}