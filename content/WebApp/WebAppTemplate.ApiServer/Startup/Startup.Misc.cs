namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private static void SetupStorage()
    {
        Directory.CreateDirectory("storage");
    }
}