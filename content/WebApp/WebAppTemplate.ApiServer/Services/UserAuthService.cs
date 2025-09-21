using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using MoonCore.Helpers;
using WebAppTemplate.ApiServer.Database.Entities;

namespace WebAppTemplate.ApiServer.Services;

public class UserAuthService
{
    private readonly ILogger<UserAuthService> Logger;
    private readonly DatabaseRepository<User> UserRepository;

    private const string UserIdClaim = "UserId";
    private const string IssuedAtClaim = "IssuedAt";

    public UserAuthService(ILogger<UserAuthService> logger, DatabaseRepository<User> userRepository)
    {
        Logger = logger;
        UserRepository = userRepository;
    }

    public async Task<bool> SyncAsync(ClaimsPrincipal? principal)
    {
        // Ignore malformed claims principal
        if (principal is not { Identity.IsAuthenticated: true })
            return false;

        // Search for email and username. We need both to create the user model
        
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
            user = await UserRepository.AddAsync(new User()
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
            await UserRepository.UpdateAsync(user);
        }

        principal.Identities.First().AddClaims([
            new Claim(UserIdClaim, user.Id.ToString()),
            new Claim(IssuedAtClaim, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        ]);

        return true;
    }

    public async Task<bool> ValidateAsync(ClaimsPrincipal? principal)
    {
        // Ignore malformed claims principal
        if(principal is not { Identity.IsAuthenticated: true })
            return false;
        
        // Validate if the user still exists, and then we want to validate the token issue time
        // against the invalidation time

        var userIdStr = principal.FindFirstValue(UserIdClaim);

        if (!int.TryParse(userIdStr, out var userId))
            return false;

        var user = await UserRepository
            .Get()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return false;

        // Token time validation
        var issuedAtStr = principal.FindFirstValue(IssuedAtClaim);
        
        if(!long.TryParse(issuedAtStr, out var issuedAtUnix))
            return false;
        
        var issuedAt = DateTimeOffset
            .FromUnixTimeSeconds(issuedAtUnix)
            .ToUniversalTime();
        
        // If the issued at timestamp is greater than the token validation timestamp
        // everything is fine. If not it means that the token should be invalidated
        // as it is too old
        
        return issuedAt > user.InvalidateTimestamp;
    }
}