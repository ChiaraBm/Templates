using System.Text;
using Microsoft.IdentityModel.Tokens;
using WebAppTemplate.ApiServer.Implementations.LocalAuth;
using WebAppTemplate.ApiServer.Services;

namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private Task RegisterAuth()
    {
        WebApplicationBuilder.Services
            .AddAuthentication(options => { options.DefaultScheme = "MainScheme"; })
            .AddPolicyScheme("MainScheme", null, options =>
            {
                // If an api key is specified via the bearer auth header
                // we want to use the ApiKey scheme for authenticating the request
                options.ForwardDefaultSelector = context =>
                {
                    if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                        return "Session";

                    var auth = authHeader.FirstOrDefault();

                    if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer "))
                        return "Session";

                    return "ApiKey";
                };
            })
            .AddJwtBearer("ApiKey", null, options =>
            {
                options.TokenValidationParameters = new()
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        Configuration.Authentication.Secret
                    )),
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateAudience = true,
                    ValidAudience = Configuration.PublicUrl,
                    ValidateIssuer = true,
                    ValidIssuer = Configuration.PublicUrl
                };
            })
            .AddCookie("Session", null, options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(Configuration.Authentication.Sessions.ExpiresIn);

                // As redirects won't work in our spa which uses API calls
                // we need to customize the responses when certain actions happen
                options.Events.OnRedirectToLogin = async context =>
                {
                    await Results.Problem(
                            title: "Unauthenticated",
                            detail: "You need to authenticate yourself to use this endpoint",
                            statusCode: 401
                        )
                        .ExecuteAsync(context.HttpContext);
                };

                options.Events.OnRedirectToAccessDenied = async context =>
                {
                    await Results.Problem(
                            title: "Permission denied",
                            detail: "You are missing the required permissions to access this endpoint",
                            statusCode: 403
                        )
                        .ExecuteAsync(context.HttpContext);
                };

                options.Events.OnSigningIn = async context =>
                {
                    var userSyncService = context
                        .HttpContext
                        .RequestServices
                        .GetRequiredService<UserAuthService>();

                    var result = await userSyncService.Sync(context.Principal);

                    if (!result)
                        context.Principal = new();
                };

                options.Events.OnValidatePrincipal = async context =>
                {
                    var userSyncService = context
                        .HttpContext
                        .RequestServices
                        .GetRequiredService<UserAuthService>();

                    var result = await userSyncService.Validate(context.Principal);

                    if (!result)
                        context.RejectPrincipal();
                };

                options.Cookie = new CookieBuilder()
                {
                    Name = Configuration.Authentication.Sessions.CookieName,
                    Path = "/",
                    HttpOnly = true
                };
            })
            .AddScheme<LocalAuthOptions, LocalAuthHandler>(LocalAuthConstants.AuthenticationScheme, null, options =>
            {
                options.ForwardAuthenticate = "Session";
                options.ForwardSignIn = "Session";
                options.ForwardSignOut = "Session";

                options.SignInScheme = "Session";
            });

        WebApplicationBuilder.Services.AddAuthorization();

        WebApplicationBuilder.Services.AddScoped<UserAuthService>();

        return Task.CompletedTask;
    }

    private Task UseAuth()
    {
        WebApplication.UseAuthentication();
        WebApplication.UseAuthorization();

        return Task.CompletedTask;
    }
}