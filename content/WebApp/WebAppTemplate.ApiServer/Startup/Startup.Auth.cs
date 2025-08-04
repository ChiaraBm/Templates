using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MoonCore.Extended.JwtInvalidation;
using WebAppTemplate.ApiServer.Implementations;

namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private Task RegisterAuth()
    {
        WebApplicationBuilder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new()
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        Configuration.Authentication.Secret
                    )),
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateAudience = true,
                    ValidAudience = Configuration.PublicUrl,
                    ValidateIssuer = true,
                    ValidIssuer = Configuration.PublicUrl
                };
            });

        WebApplicationBuilder.Services.AddJwtBearerInvalidation();
        WebApplicationBuilder.Services.AddScoped<IJwtInvalidateHandler, UserJwtInvalidator>();

        WebApplicationBuilder.Services.AddAuthorization();
        
        return Task.CompletedTask;
    }

    private Task UseAuth()
    {
        WebApplication.UseAuthentication();
        WebApplication.UseAuthorization();
        
        return Task.CompletedTask;
    }
}