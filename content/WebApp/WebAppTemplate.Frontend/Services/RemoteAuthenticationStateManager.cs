using Microsoft.AspNetCore.Components.Authorization;
using MoonCore.Blazor.Tailwind.Auth;

namespace WebAppTemplate.Frontend.Services;

public class RemoteAuthenticationStateManager : AuthenticationStateManager
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        throw new NotImplementedException();
    }

    public override Task HandleLogin()
    {
        throw new NotImplementedException();
    }

    public override Task Logout()
    {
        throw new NotImplementedException();
    }
}