using System.Security.Claims;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MoonCore.Blazor.Services;
using MoonCore.Blazor.Tailwind.Auth;
using MoonCore.Helpers;

namespace WebAppTemplate.Frontend.Services;

public class RemoteAuthStateManager : AuthenticationStateManager
{
    private readonly NavigationManager NavigationManager;
    private readonly HttpApiClient HttpApiClient;
    private readonly LocalStorageService LocalStorageService;

    public RemoteAuthStateManager(
        HttpApiClient httpApiClient,
        LocalStorageService localStorageService,
        NavigationManager navigationManager
    )
    {
        HttpApiClient = httpApiClient;
        LocalStorageService = localStorageService;
        NavigationManager = navigationManager;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return new AuthenticationState(new ClaimsPrincipal(
            new ClaimsIdentity([
                new Claim("Name", "Test")
            ], "RemoteAuthStateManager"))
        );
    }

    public override async Task HandleLogin()
    {
        var uri = new Uri(NavigationManager.Uri);
        var codeParam = HttpUtility.ParseQueryString(uri.Query).Get("code");

        if (string.IsNullOrEmpty(codeParam)) // If this is true, we need to log in the user
        {
        }
    }

    public override async Task Logout()
    {
        if (await LocalStorageService.ContainsKey("AccessToken"))
            await LocalStorageService.SetString("AccessToken", "");
    }
}