using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MoonCore.Blazor.FlyonUi.Exceptions;
using WebAppTemplate.Frontend.Implementations;
using WebAppTemplate.Frontend.Services;

namespace WebAppTemplate.Frontend.Startup;


public partial class Startup
{
    private static void AddAuthentication(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();
        
        builder.Services.AddScoped<AuthenticationStateProvider, RemoteAuthStateProvider>();

        builder.Services.AddScoped<IGlobalErrorFilter, UnauthenticatedErrorFilter>();
    }
}