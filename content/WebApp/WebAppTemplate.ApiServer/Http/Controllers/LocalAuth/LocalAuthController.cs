using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using MoonCore.Helpers;
using WebAppTemplate.ApiServer.Database.Entities;
using WebAppTemplate.ApiServer.Implementations.LocalAuth;

namespace WebAppTemplate.ApiServer.Http.Controllers.LocalAuth;

[ApiController]
[Route("api/localauth")]
public class LocalAuthController : Controller
{
    private readonly DatabaseRepository<User> UserRepository;
    private readonly IServiceProvider ServiceProvider;
    private readonly IAuthenticationService AuthenticationService;
    private readonly IOptionsMonitor<LocalAuthOptions> Options;
    private readonly ILogger<LocalAuthController> Logger;

    public LocalAuthController(
        DatabaseRepository<User> userRepository,
        IServiceProvider serviceProvider,
        IAuthenticationService authenticationService,
        IOptionsMonitor<LocalAuthOptions> options,
        ILogger<LocalAuthController> logger
    )
    {
        UserRepository = userRepository;
        ServiceProvider = serviceProvider;
        AuthenticationService = authenticationService;
        Options = options;
        Logger = logger;
    }

    [HttpGet]
    [HttpGet("login")]
    public async Task<IResult> Login()
    {
        var html = await ComponentHelper.RenderComponent<Login>(ServiceProvider);

        return Results.Content(html, "text/html");
    }

    [HttpGet("register")]
    public async Task<IResult> Register()
    {
        var html = await ComponentHelper.RenderComponent<Register>(ServiceProvider);

        return Results.Content(html, "text/html");
    }

    [HttpPost]
    [HttpPost("login")]
    public async Task<IResult> Login([FromForm] string email, [FromForm] string password)
    {
        try
        {
            // Perform login
            var user = await InternalLogin(email, password);

            // Login user
            var options = Options.Get(LocalAuthConstants.AuthenticationScheme);

            await AuthenticationService.SignInAsync(HttpContext, options.SignInScheme, new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Username)
                    ],
                    LocalAuthConstants.AuthenticationScheme
                )
            ), new AuthenticationProperties());

            // Redirect back to wasm app
            return Results.Redirect("/");
        }
        catch (Exception e)
        {
            string errorMessage;

            if (e is HttpApiException apiException)
                errorMessage = apiException.Title;
            else
            {
                errorMessage = "An internal error occured";
                Logger.LogError(e, "An unhandled error occured while logging in user");
            }

            var html = await ComponentHelper.RenderComponent<Login>(ServiceProvider,
                parameters => { parameters["ErrorMessage"] = errorMessage; });

            return Results.Content(html, "text/html");
        }
    }

    [HttpPost("register")]
    public async Task<IResult> Register([FromForm] string email, [FromForm] string password, [FromForm] string username)
    {
        try
        {
            // Perform register
            var user = await InternalRegister(username, email, password);

            // Login user
            var options = Options.Get(LocalAuthConstants.AuthenticationScheme);

            await AuthenticationService.SignInAsync(HttpContext, options.SignInScheme, new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Username)
                    ],
                    LocalAuthConstants.AuthenticationScheme
                )
            ), new AuthenticationProperties());

            // Redirect back to wasm app
            return Results.Redirect("/");
        }
        catch (Exception e)
        {
            string errorMessage;

            if (e is HttpApiException apiException)
                errorMessage = apiException.Title;
            else
            {
                errorMessage = "An internal error occured";
                Logger.LogError(e, "An unhandled error occured while logging in user");
            }

            var html = await ComponentHelper.RenderComponent<Register>(ServiceProvider,
                parameters => { parameters["ErrorMessage"] = errorMessage; });

            return Results.Content(html, "text/html");
        }
    }

    private async Task<User> InternalRegister(string username, string email, string password)
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

    private async Task<User> InternalLogin(string email, string password)
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