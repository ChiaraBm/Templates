namespace WebAppTemplate.ApiServer.Startup;

public static partial class Startup
{
    public static void AddWebAppTemplate(this WebApplicationBuilder builder)
    {
        SetupStorage();
        builder.AddAppConfiguration();
        
        builder.AddLogging();
        builder.AddBase();
        builder.AddDatabase();
        builder.AddAuth();
    }
    
    public static void UseWebAppTemplate(this WebApplication app)
    {
        app.UseBase();
        app.UseAuth();
        
        app.MapBase();
    }
}