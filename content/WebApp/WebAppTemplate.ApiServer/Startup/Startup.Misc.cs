namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private Task SetupStorage()
    {
        Directory.CreateDirectory("storage");
        Directory.CreateDirectory(Path.Combine("storage", "logs"));

        return Task.CompletedTask;
    }
}