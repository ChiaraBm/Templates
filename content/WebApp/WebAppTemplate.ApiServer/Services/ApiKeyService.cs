using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WebAppTemplate.ApiServer.Configuration;

namespace WebAppTemplate.ApiServer.Services;

public class ApiKeyService
{
    private readonly AppConfiguration Configuration;

    public ApiKeyService(AppConfiguration configuration)
    {
        Configuration = configuration;
    }

    public Task<string> Generate(Action<Dictionary<string, object>> onConfigure, DateTimeOffset validUntil)
    {
        var claims = new Dictionary<string, object>();

        onConfigure(claims);

        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Audience = Configuration.PublicUrl,
            Issuer = Configuration.PublicUrl,
            Expires = validUntil.UtcDateTime,
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(Configuration.Authentication.Secret)
                ),
                SecurityAlgorithms.HmacSha256
            ),
            Claims = claims
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return Task.FromResult(
            tokenHandler.WriteToken(token)
        );
    }
}