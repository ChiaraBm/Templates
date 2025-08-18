using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using WebAppTemplate.ApiServer.Database.Entities;
using WebAppTemplate.ApiServer.Implementations.LocalAuth;

namespace WebAppTemplate.ApiServer.Implementations;

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
        if(principal.Identity is not { IsAuthenticated: true })
            return false;
        
        User? user = null;

        switch (principal.Identity.AuthenticationType)
        {
            case LocalAuthConstants.AuthenticationScheme:
                if(!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                    return false;
                
                user = await UserRepository
                    .Get()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);
                
                // Other auth schemes which aren't directly interfacing with the user repository
                // would need to create / update / lookup the user here
                
                break;
            
            // Add handlers for other auth schemes here
        }

        return user != null;
    }
}