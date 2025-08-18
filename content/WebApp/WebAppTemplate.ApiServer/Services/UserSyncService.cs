using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using MoonCore.Helpers;
using WebAppTemplate.ApiServer.Database.Entities;

namespace WebAppTemplate.ApiServer.Services;

public class UserSyncService
{
    private readonly ILogger<UserSyncService> Logger;
    private readonly DatabaseRepository<User> UserRepository;

    public UserSyncService(ILogger<UserSyncService> logger, DatabaseRepository<User> userRepository)
    {
        Logger = logger;
        UserRepository = userRepository;
    }

    public async Task<bool> Sync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not { IsAuthenticated: true })
            return false;

        var email = principal.FindFirstValue(ClaimTypes.Email)?.ToLower();
        var username = principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username))
        {
            Logger.LogWarning(
                "The authentication scheme {scheme} did not provide claim types: email, name. These are required to sync to user to the database",
                principal.Identity.AuthenticationType
            );

            return false;
        }

        // If you plan to use multiple auth providers it can be a good idea
        // to use an identifier in the user model which consists of the provider and the NameIdentifier
        // instead of the email address. For simplicity, we just use the email as the identifier so multiple auth providers
        // can lead to the same account when the email matches
        var user = await UserRepository
            .Get()
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            user = await UserRepository.Add(new User()
            {
                Email = email,
                InvalidateTimestamp = DateTimeOffset.UtcNow.AddMinutes(-1),
                Username = username,
                Password = HashHelper.Hash(Formatter.GenerateString(64))
            });
        }

        // You can sync other properties here
        if (user.Username != username)
        {
            user.Username = username;
            await UserRepository.Update(user);
        }

        principal.Identities.First().AddClaim(
            new Claim("UserId", user.Id.ToString())
        );

        return true;
    }
}