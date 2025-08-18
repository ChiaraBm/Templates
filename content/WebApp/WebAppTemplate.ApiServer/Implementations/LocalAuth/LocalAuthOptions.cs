using Microsoft.AspNetCore.Authentication;

namespace WebAppTemplate.ApiServer.Implementations.LocalAuth;

public class LocalAuthOptions : AuthenticationSchemeOptions
{
    public string? SignInScheme { get; set; }
}