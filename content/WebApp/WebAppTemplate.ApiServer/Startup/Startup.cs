using WebAppTemplate.ApiServer.Configuration;

namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private string[] Args;

    // Configuration
    private AppConfiguration Configuration;

    // WebApplication Stuff
    private WebApplication WebApplication;
    private WebApplicationBuilder WebApplicationBuilder;

    public Task InitializeAsync(string[] args)
    {
        Args = args;

        return Task.CompletedTask;
    }

    public async Task AddWebAppTemplateAsync(WebApplicationBuilder builder)
    {
        WebApplicationBuilder = builder;

        await SetupStorageAsync();
        await SetupAppConfigurationAsync();

        await RegisterAppConfigurationAsync();
        await RegisterLoggingAsync();
        await RegisterBaseAsync();
        await RegisterDatabaseAsync();
        await RegisterAuthAsync();
    }

    public async Task AddWebAppTemplateAsync(WebApplication application)
    {
        WebApplication = application;

        await PrepareDatabaseAsync();

        await UseBaseAsync();
        await UseAuthAsync();
        await UseBaseMiddlewareAsync();

        await MapBaseAsync();
    }
}