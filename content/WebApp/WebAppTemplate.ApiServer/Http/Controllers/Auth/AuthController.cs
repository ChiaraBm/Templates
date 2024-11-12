using Microsoft.AspNetCore.Mvc;
using MoonCore.Attributes;
using MoonCore.Extensions;
using WebAppTemplate.ApiServer.Database.Entities;
using WebAppTemplate.Shared.Http.Responses.Auth;

namespace WebAppTemplate.ApiServer.Http.Controllers.Auth;

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