using DesktopApp;

public class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        var startup = new Startup();

        await startup.Run(args);
    }
}