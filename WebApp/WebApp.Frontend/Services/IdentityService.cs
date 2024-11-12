using MoonCore.Attributes;
using MoonCore.Blazor.Services;
using MoonCore.Helpers;
using WebApp.Shared.Http.Responses.Auth;

namespace WebApp.Frontend.Services;

[Scoped]
public class IdentityService
{
    public string Username { get; private set; } = "";
    public string Email { get; private set; } = "";
    
    private readonly HttpApiClient HttpApiClient;
    private readonly LocalStorageService LocalStorageService;

    public IdentityService(HttpApiClient httpApiClient, LocalStorageService localStorageService)
    {
        HttpApiClient = httpApiClient;
        LocalStorageService = localStorageService;
    }

    public async Task Check()
    {
        var response = await HttpApiClient.GetJson<CheckResponse>("api/auth/check");

        Username = response.Username;
        Email = response.Email;
    }

    public async Task Logout()
    {
        await LocalStorageService.SetString("AccessToken", "unset");
        await LocalStorageService.SetString("RefreshToken", "unset");
        await LocalStorageService.Set("ExpiresAt", DateTime.MinValue);
    }
}