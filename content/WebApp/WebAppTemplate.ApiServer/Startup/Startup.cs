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

    public Task Initialize(string[] args)
    {
        Args = args;

        return Task.CompletedTask;
    }

    public async Task AddWebAppTemplate(WebApplicationBuilder builder)
    {
        WebApplicationBuilder = builder;

        await SetupStorage();
        await SetupAppConfiguration();

        await RegisterAppConfiguration();
        await RegisterLogging();
        await RegisterBase();
        await RegisterDatabase();
        await RegisterAuth();
    }

    public async Task AddWebAppTemplate(WebApplication application)
    {
        WebApplication = application;

        await PrepareDatabase();

        await UseBase();
        await UseAuth();
        await UseBaseMiddleware();

        await MapBase();
    }
}