using MoonCore.Helpers;
using YamlDotNet.Serialization;

namespace WebAppTemplate.ApiServer.Configuration;

public class AppConfiguration
{
    [YamlMember(Description = "The url WebAppTemplate is ")]
    public string PublicUrl { get; set; } = "http://localhost:5265";
    
    public DatabaseConfig Database { get; set; } = new();
    public AuthenticationConfig Authentication { get; set; } = new();
    
    public class DatabaseConfig
    {
        public string Host { get; set; } = "your-database-host.name";
        public int Port { get; set; } = 5432;

        public string Username { get; set; } = "db_user";
        public string Password { get; set; } = "db_password";

        public string Database { get; set; } = "db_name";
    }
    
    public class AuthenticationConfig
    {
        public string Secret { get; set; } = Formatter.GenerateString(32);
        public string ClientId { get; set; } = Formatter.GenerateString(8);
        public string ClientSecret { get; set; } = Formatter.GenerateString(32);
        public string? RedirectUri { get; set; }
        public string? AuthorizeEndpoint { get; set; }
    }
}