namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private Task SetupStorageAsync()
    {
        Directory.CreateDirectory("storage");

        return Task.CompletedTask;
    }
}