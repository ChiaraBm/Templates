using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using MoonCore.Extended.OAuth2.LocalProvider;
using WebAppTemplate.ApiServer.Database.Entities;

namespace WebAppTemplate.ApiServer.Implementations.OAuth2;

public class LocalUserOAuth2Provider : ILocalProviderImplementation<User>
{
    private readonly DatabaseRepository<User> UserRepository;

    public LocalUserOAuth2Provider(DatabaseRepository<User> userRepository)
    {
        UserRepository = userRepository;
    }

    public async Task SaveChanges(User model)
    {
        await UserRepository.Update(model);
    }

    public async Task<User?> LoadById(int id)
    {
        var nullableUser = await UserRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        return nullableUser;
    }

    public async Task<User> Login(string email, string password)
    {
        var user = await UserRepository.Get().FirstOrDefaultAsync(x => x.Email == email);

        if (user == null)
            throw new HttpApiException("Invalid email or password", 400);
        
        if(!HashHelper.Verify(password, user.Password))
            throw new HttpApiException("Invalid email or password", 400);
        
        return user;
    }

    public async Task<User> Register(string username, string email, string password)
    {
        if (UserRepository.Get().Any(x => x.Username == username))
            throw new HttpApiException("A user with that username already exists", 400);

        if (UserRepository.Get().Any(x => x.Email == email))
            throw new HttpApiException("A user with that email address already exists", 400);

        var user = new User()
        {
            Username = username,
            Email = email,
            Password = HashHelper.Hash(password)
        };

        var finalUser = await UserRepository.Add(user);
        
        return finalUser;
    }
}