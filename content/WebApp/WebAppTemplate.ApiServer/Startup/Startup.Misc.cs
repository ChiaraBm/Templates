namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private Task SetupStorage()
    {
        Directory.CreateDirectory("storage");

        return Task.CompletedTask;
    }
}