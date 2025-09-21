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
    public async Task<ActionResult> LoginAsync()
    {
        var html = await ComponentHelper.RenderToHtmlAsync<Login>(ServiceProvider);

        return Content(html, "text/html");
    }

    [HttpGet("register")]
    public async Task<ActionResult> RegisterAsync()
    {
        var html = await ComponentHelper.RenderToHtmlAsync<Register>(ServiceProvider);

        return Content(html, "text/html");
    }

    [HttpPost]
    [HttpPost("login")]
    public async Task<ActionResult> LoginAsync([FromForm] string email, [FromForm] string password)
    {
        try
        {
            // Perform login
            var user = await InternalLoginAsync(email, password);

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
            return Redirect("/");
        }
        catch (Exception e)
        {
            string errorMessage;

            if (e is AggregateException aggregateException)
                errorMessage = aggregateException.Message;
            else
            {
                errorMessage = "An internal error occured";
                Logger.LogError(e, "An unhandled error occured while logging in user");
            }

            var html = await ComponentHelper.RenderToHtmlAsync<Login>(ServiceProvider,
                parameters => { parameters["ErrorMessage"] = errorMessage; });

            return Content(html, "text/html");
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult> RegisterAsync([FromForm] string email, [FromForm] string password, [FromForm] string username)
    {
        try
        {
            // Perform register
            var user = await InternalRegisterAsync(username, email, password);

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
            return Redirect("/");
        }
        catch (Exception e)
        {
            string errorMessage;

            if (e is AggregateException aggregateException)
                errorMessage = aggregateException.Message;
            else
            {
                errorMessage = "An internal error occured";
                Logger.LogError(e, "An unhandled error occured while logging in user");
            }

            var html = await ComponentHelper.RenderToHtmlAsync<Register>(ServiceProvider,
                parameters => { parameters["ErrorMessage"] = errorMessage; });

            return Content(html, "text/html");
        }
    }

    private async Task<User> InternalRegisterAsync(string username, string email, string password)
    {
        if (await UserRepository.Get().AnyAsync(x => x.Username == username))
            throw new AggregateException("A account with that username already exists");

        if (await UserRepository.Get().AnyAsync(x => x.Email == email))
            throw new AggregateException("A account with that email already exists");

        var user = new User()
        {
            Username = username,
            Email = email,
            Password = HashHelper.Hash(password)
        };

        var finalUser = await UserRepository.AddAsync(user);

        return finalUser;
    }

    private async Task<User> InternalLoginAsync(string email, string password)
    {
        var user = await UserRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user == null)
            throw new AggregateException("Invalid combination of email and password");

        if (!HashHelper.Verify(password, user.Password))
            throw new AggregateException("Invalid combination of email and password");

        return user;
    }
}