using MoonCore.Helpers;

namespace WebAppTemplate.ApiServer.Configuration;

public class AppConfiguration
{
    public string PublicUrl { get; set; } = "http://localhost:5265";
    
    public DatabaseConfig Database { get; set; } = new();
    public AuthenticationConfig Authentication { get; set; } = new();
    
    public class DatabaseConfig
    {
        public string Host { get; set; } = "your-database-host.name";
        public int Port { get; set; } = 3306;

        public string Username { get; set; } = "db_user";
        public string Password { get; set; } = "db_password";

        public string Database { get; set; } = "db_name";
    }
    
    public class AuthenticationConfig
    {
        public string AccessSecret { get; set; } = Formatter.GenerateString(32);
        public string RefreshSecret { get; set; } = Formatter.GenerateString(32);
        public int RefreshInterval { get; set; } = 60;
        public int RefreshDuration { get; set; } = 3600;
        public string ClientId { get; set; } = Formatter.GenerateString(8);
        public string ClientSecret { get; set; } = Formatter.GenerateString(32);
        public string? RedirectUri { get; set; }
        public string? AuthorizeEndpoint { get; set; }
        public bool UseLocalOAuth2 { get; set; } = true;
    }
}