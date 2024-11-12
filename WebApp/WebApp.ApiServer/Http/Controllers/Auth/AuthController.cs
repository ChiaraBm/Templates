using Microsoft.AspNetCore.Mvc;
using MoonCore.Attributes;
using MoonCore.Extensions;
using WebApp.ApiServer.Database.Entities;
using WebApp.Shared.Http.Responses.Auth;

namespace WebApp.ApiServer.Http.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : Controller
{
    [HttpGet("check")]
    [RequirePermission("meta.authenticated")]
    public async Task<CheckResponse> Check()
    {
        var user = HttpContext.User.AsIdentity<User>();
        
        return new CheckResponse()
        {
            Email = user.Email,
            Username = user.Username
        };
    }
}