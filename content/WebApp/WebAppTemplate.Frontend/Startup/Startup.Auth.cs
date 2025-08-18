using Microsoft.AspNetCore.Components.Authorization;
using MoonCore.Blazor.FlyonUi.Exceptions;
using WebAppTemplate.Frontend.Implementations;
using WebAppTemplate.Frontend.Services;

namespace WebAppTemplate.Frontend.Startup;


public partial class Startup
{
    private Task RegisterAuthentication()
    {
        WebAssemblyHostBuilder.Services.AddAuthorizationCore();
        WebAssemblyHostBuilder.Services.AddCascadingAuthenticationState();
        
        WebAssemblyHostBuilder.Services.AddScoped<AuthenticationStateProvider, RemoteAuthStateProvider>();

        WebAssemblyHostBuilder.Services.AddScoped<IGlobalErrorFilter, UnauthenticatedErrorFilter>();
        
        return Task.CompletedTask;
    }
}