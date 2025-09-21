using MoonCore.EnvConfiguration;
using MoonCore.Yaml;
using WebAppTemplate.ApiServer.Configuration;

namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private async Task SetupAppConfigurationAsync()
    {
        // Configure configuration (wow)
        var configurationBuilder = new ConfigurationBuilder();
        
        // Ensure configuration file exists
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "storage", "config.yml");

        await YamlDefaultGenerator.GenerateAsync<AppConfiguration>(configPath);

        configurationBuilder.AddYamlFile(configPath);
        configurationBuilder.AddEnvironmentVariables(prefix: "WebAppTemplate_".ToUpper(), separator: "_");

        var configurationRoot = configurationBuilder.Build();

        // Retrieve configuration
        Configuration = AppConfiguration.CreateEmpty();
        configurationRoot.Bind(Configuration);
    }

    private Task RegisterAppConfigurationAsync()
    {
        WebApplicationBuilder.Services.AddSingleton(Configuration);
        return Task.CompletedTask;
    }
}