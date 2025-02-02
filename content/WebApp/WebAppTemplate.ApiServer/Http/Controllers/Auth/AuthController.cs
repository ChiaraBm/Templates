using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoonCore.Extended.Abstractions;
using MoonCore.Extensions;
using WebAppTemplate.ApiServer.Configuration;
using WebAppTemplate.ApiServer.Database.Entities;
using WebAppTemplate.Shared.Http.Requests.Auth;
using WebAppTemplate.Shared.Http.Responses.Auth;

namespace WebAppTemplate.ApiServer.Http.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : Controller
{
    private readonly AppConfiguration Configuration;
    private readonly DatabaseRepository<User> User;

    public AuthController(AppConfiguration configuration, DatabaseRepository<User> user)
    {
        Configuration = configuration;
        User = user;
    }

    [AllowAnonymous]
    [HttpGet("start")]
    public async Task<LoginStartResponse> Start()
    {
        return new LoginStartResponse()
        {
            ClientId = Configuration.Authentication.ClientId,
            RedirectUri = Configuration.Authentication.RedirectUri ?? Configuration.PublicUrl,
            Endpoint = Configuration.Authentication.AuthorizeEndpoint ?? Configuration.Authentication + "/oauth/authorize"
        };
    }

    [AllowAnonymous]
    [HttpPost("complete")]
    public async Task<LoginCompleteResponse> Complete([FromBody] LoginCompleteRequest request)
    {
        return new();
    }
    
    [HttpGet("check")]
    [Authorize]
    public async Task<CheckResponse> Check()
    {
        return new();
    }
}