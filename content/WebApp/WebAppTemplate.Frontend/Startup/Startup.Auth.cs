using Microsoft.AspNetCore.Components.Authorization;
using WebAppTemplate.Frontend.Services;

namespace WebAppTemplate.Frontend.Startup;


public partial class Startup
{
    private Task RegisterAuthentication()
    {
        WebAssemblyHostBuilder.Services.AddAuthorizationCore();
        WebAssemblyHostBuilder.Services.AddCascadingAuthenticationState();
        
        WebAssemblyHostBuilder.Services.AddScoped<AuthenticationStateProvider, RemoteAuthStateProvider>();
        
        return Task.CompletedTask;
    }
}