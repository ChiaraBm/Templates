using MoonCore.Helpers;
using YamlDotNet.Serialization;

namespace WebAppTemplate.ApiServer.Configuration;

public class AppConfiguration
{
    [YamlMember(Description = "The url WebAppTemplate is accessible through")]
    public string PublicUrl { get; set; } = "http://localhost:5266";
    
    [YamlMember(Description = "\nThe postgres database credentials the application should use")]
    public DatabaseConfig Database { get; set; } = new();
    
    [YamlMember(Description = "\nThe authentication and oauth2 settings to use for the login flow")]
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

        [YamlMember(Description = "\nThis section configures the behavior of user sessions")]
        public SessionsConfig Sessions { get; set; } = new();
    }
    
    public class SessionsConfig
    {
        [YamlMember(Description = "Specifies the cookie name used to save the session on the useragent")]
        public string CookieName { get; set; } = "session";
        
        [YamlMember(Description = "Sets the expiry time of the cookie in days")]
        public int ExpiresIn { get; set; } = 10;
    }

    public static AppConfiguration CreateEmpty()
    {
        return new()
        {
            // Empty default values for arrays here to ensure the microsoft config system doesn't duplicate entries
        };
    }
}