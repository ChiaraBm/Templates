using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.JwtInvalidation;
using WebAppTemplate.ApiServer.Database.Entities;

namespace WebAppTemplate.ApiServer.Implementations;

public class UserJwtInvalidator : IJwtInvalidateHandler
{
    private readonly DatabaseRepository<User> UserRepository;

    public UserJwtInvalidator(DatabaseRepository<User> userRepository)
    {
        UserRepository = userRepository;
    }

    public async Task<bool> Handle(ClaimsPrincipal principal)
    {
        var issuedAtStr = principal.FindFirstValue("iat")!;
        var issuedUnix = long.Parse(issuedAtStr);
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedUnix);
        
        var userIdClaim = principal.FindFirstValue("userId")!;
        var userId = int.Parse(userIdClaim);
                
        var user = await UserRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == userId);
        
        // If no user has been found with that id, we invalidate the jwt
        if(user == null)
            return true;

        return user.InvalidateTimestamp > issuedAt; // When the invalidate timestamp is higher than the iat timestamp, the jwt is expired
    }
}