using MoonCore.EnvConfiguration;
using MoonCore.Yaml;
using WebAppTemplate.ApiServer.Configuration;

namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private static void AddAppConfiguration(this WebApplicationBuilder builder)
    {
        // Ensure configuration file exists
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "storage", "config.yml");

        YamlDefaultGenerator.Generate<AppConfiguration>(configPath);

        builder.Configuration.AddYamlFile(configPath);
        builder.Configuration.AddEnvironmentVariables(prefix: "WebAppTemplate_".ToUpper(), separator: "_");

        // Retrieve configuration
        var configuration = AppConfiguration.CreateEmpty();
        builder.Configuration.Bind(configuration);

        builder.Services.AddSingleton(configuration);
    }
}