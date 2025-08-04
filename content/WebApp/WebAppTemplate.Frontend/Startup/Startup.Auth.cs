using WebAppTemplate.Frontend.Services;
using MoonCore.Blazor.FlyonUi.Auth;

namespace WebAppTemplate.Frontend.Startup;


public partial class Startup
{
    private Task RegisterAuthentication()
    {
        WebAssemblyHostBuilder.Services.AddAuthorizationCore();
        WebAssemblyHostBuilder.Services.AddCascadingAuthenticationState();
        
        WebAssemblyHostBuilder.Services.AddAuthenticationStateManager<RemoteAuthStateManager>();
        
        return Task.CompletedTask;
    }
}