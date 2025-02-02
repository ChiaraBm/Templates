using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using MoonCore.Helpers;
using WebAppTemplate.ApiServer.Configuration;
using WebAppTemplate.ApiServer.Database.Entities;

namespace WebAppTemplate.ApiServer.Http.Controllers.OAuth2;

[ApiController]
[Route("oauth2")]
public class OAuth2Controller : Controller
{
    private readonly AppConfiguration Configuration;
    private readonly DatabaseRepository<User> UserRepository;

    public OAuth2Controller(AppConfiguration configuration, DatabaseRepository<User> userRepository)
    {
        Configuration = configuration;
        UserRepository = userRepository;
    }

    [AllowAnonymous]
    [HttpGet("authorize")]
    public async Task Authorize(
        [FromQuery(Name = "client_id")] string clientId,
        [FromQuery(Name = "redirect_uri")] string redirectUri,
        [FromQuery(Name = "response_type")] string responseType,
        [FromQuery(Name = "view")] string view = "login"
    )
    {
        var requiredRedirectUri = Configuration.Authentication.RedirectUri ?? Configuration.PublicUrl;

        if (Configuration.Authentication.ClientId != clientId ||
            requiredRedirectUri != redirectUri ||
            responseType != "code")
        {
            throw new HttpApiException("Invalid oauth2 request", 400);
        }

        Response.StatusCode = 200;

        if (view == "register")
        {
            var html = await ComponentHelper.RenderComponent<Register>(HttpContext.RequestServices, parameters =>
            {
                parameters.Add("ClientId", clientId);
                parameters.Add("RedirectUri", redirectUri);
                parameters.Add("ResponseType", responseType);
            });

            await Response.WriteAsync(html);
        }
        else
        {
            var html = await ComponentHelper.RenderComponent<Login>(HttpContext.RequestServices, parameters =>
            {
                parameters.Add("ClientId", clientId);
                parameters.Add("RedirectUri", redirectUri);
                parameters.Add("ResponseType", responseType);
            });

            await Response.WriteAsync(html);
        }
    }

    [AllowAnonymous]
    [HttpPost("authorize")]
    public async Task AuthorizePost(
        [FromQuery(Name = "client_id")] string clientId,
        [FromQuery(Name = "redirect_uri")] string redirectUri,
        [FromQuery(Name = "response_type")] string responseType,
        [FromForm(Name = "email")] string email,
        [FromForm(Name = "password")] string password,
        [FromForm(Name = "username")] string username = "",
        [FromQuery(Name = "view")] string view = "login"
    )
    {
        var requiredRedirectUri = Configuration.Authentication.RedirectUri ?? Configuration.PublicUrl;

        if (Configuration.Authentication.ClientId != clientId ||
            requiredRedirectUri != redirectUri ||
            responseType != "code")
        {
            throw new HttpApiException("Invalid oauth2 request", 400);
        }

        if (view == "register" && string.IsNullOrEmpty(username))
            throw new HttpApiException("You need to provide a username", 400);

        string? errorMessage = null;

        try
        {
            if (view == "register")
            {
                var user = await Register(username, email, password);
                var code = await GenerateCode(user);

                Response.Redirect($"{redirectUri}?code={code}");
                return;
            }
            else
            {
                var user = await Login(email, password);
                var code = await GenerateCode(user);

                Response.Redirect($"{redirectUri}?code={code}");
                return;
            }
        }
        catch (HttpApiException e)
        {
            errorMessage = e.Title;
        }

        Response.StatusCode = 200;

        if (view == "register")
        {
            var html = await ComponentHelper.RenderComponent<Register>(HttpContext.RequestServices, parameters =>
            {
                parameters.Add("ClientId", clientId);
                parameters.Add("RedirectUri", redirectUri);
                parameters.Add("ResponseType", responseType);
                parameters.Add("ErrorMessage", errorMessage!);
            });

            await Response.WriteAsync(html);
        }
        else
        {
            var html = await ComponentHelper.RenderComponent<Login>(HttpContext.RequestServices, parameters =>
            {
                parameters.Add("ClientId", clientId);
                parameters.Add("RedirectUri", redirectUri);
                parameters.Add("ResponseType", responseType);
                parameters.Add("ErrorMessage", errorMessage!);
            });

            await Response.WriteAsync(html);
        }
    }

    private async Task<string> GenerateCode(User user)
    {
        var securityTokenDescriptor = new SecurityTokenDescriptor()
        {
            Expires = DateTime.Now.AddMinutes(1),
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
            )
        };

        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        var securityToken = jwtSecurityTokenHandler.CreateToken(securityTokenDescriptor);

        return jwtSecurityTokenHandler.WriteToken(securityToken);
    }

    private async Task<User> Register(string username, string email, string password)
    {
        if (await UserRepository.Get().AnyAsync(x => x.Username == username))
            throw new HttpApiException("A account with that username already exists", 400);

        if (await UserRepository.Get().AnyAsync(x => x.Email == email))
            throw new HttpApiException("A account with that email already exists", 400);

        var user = new User()
        {
            Username = username,
            Email = email,
            Password = HashHelper.Hash(password)
        };

        var finalUser = await UserRepository.Add(user);

        return finalUser;
    }

    private async Task<User> Login(string email, string password)
    {
        var user = await UserRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user == null)
            throw new HttpApiException("Invalid combination of email and password", 400);

        if (!HashHelper.Verify(password, user.Password))
            throw new HttpApiException("Invalid combination of email and password", 400);

        return user;
    }
}