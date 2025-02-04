using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Helpers;
using WebAppTemplate.ApiServer.Configuration;
using WebAppTemplate.ApiServer.Database.Entities;
using WebAppTemplate.Shared.Http.Requests.Auth;
using WebAppTemplate.Shared.Http.Responses.Auth;
using WebAppTemplate.Shared.Http.Responses.OAuth2;

namespace WebAppTemplate.ApiServer.Http.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : Controller
{
    private readonly AppConfiguration Configuration;
    private readonly DatabaseRepository<User> UserRepository;
    private readonly ILogger<AuthController> Logger;

    public AuthController(
        AppConfiguration configuration,
        DatabaseRepository<User> userRepository,
        ILogger<AuthController> logger
    )
    {
        Configuration = configuration;
        UserRepository = userRepository;
        Logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("start")]
    public async Task<LoginStartResponse> Start()
    {
        return new LoginStartResponse()
        {
            ClientId = Configuration.Authentication.ClientId,
            RedirectUri = Configuration.Authentication.RedirectUri ?? Configuration.PublicUrl,
            Endpoint = Configuration.Authentication.AuthorizeEndpoint ??
                       Configuration.PublicUrl + "/oauth2/authorize"
        };
    }

    [AllowAnonymous]
    [HttpPost("complete")]
    public async Task<LoginCompleteResponse> Complete([FromBody] LoginCompleteRequest request)
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(Configuration.PublicUrl);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Configuration.Authentication.ClientSecret}");

        var httpApiClient = new HttpApiClient(httpClient);

        OAuth2HandleResponse handleData;

        try
        {
            handleData = await httpApiClient.PostJson<OAuth2HandleResponse>("oauth2/handle", new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", request.Code),
                    new KeyValuePair<string, string>("redirect_uri", Configuration.Authentication.RedirectUri ?? Configuration.PublicUrl),
                    new KeyValuePair<string, string>("client_id", Configuration.Authentication.ClientId)
                ]
            ));
        }
        catch (HttpApiException e)
        {
            if (e.Status == 400)
                Logger.LogTrace("The auth server returned an error: {e}", e);
            else
                Logger.LogCritical("The auth server returned an error: {e}", e);

            throw new HttpApiException("Unable to request user data", 500);
        }

        var userId = handleData.UserId;

        var user = await UserRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            throw new HttpApiException("Unable to load user data", 500);
        
        // Generate token
        var securityTokenDescriptor = new SecurityTokenDescriptor()
        {
            Expires = DateTime.Now.AddDays(10),
            IssuedAt = DateTime.Now,
            NotBefore = DateTime.Now.AddMinutes(-1),
            Claims = new Dictionary<string, object>()
            {
                {
                    "userId",
                    user.Id
                }
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(Configuration.Authentication.Secret)
                ),
                SecurityAlgorithms.HmacSha256
            ),
            Issuer = Configuration.PublicUrl,
            Audience = Configuration.PublicUrl
        };

        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        var securityToken = jwtSecurityTokenHandler.CreateToken(securityTokenDescriptor);

        var jwt = jwtSecurityTokenHandler.WriteToken(securityToken);

        return new()
        {
            AccessToken = jwt
        };
    }

    [HttpGet("check")]
    [Authorize]
    public async Task<CheckResponse> Check()
    {
        var userIdClaim = User.Claims.First(x => x.Type == "userId");
        var userId = int.Parse(userIdClaim.Value);
        var user = await UserRepository.Get().FirstAsync(x => x.Id == userId);
        
        return new()
        {
            Email = user.Email,
            Username = user.Username
        };
    }
}