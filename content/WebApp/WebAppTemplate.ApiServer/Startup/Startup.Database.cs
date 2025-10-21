using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Extensions;
using WebAppTemplate.ApiServer.Database;

namespace WebAppTemplate.ApiServer.Startup;

public partial class Startup
{
    private static void AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDatabaseMappings();
        builder.Services.AddDbAutoMigrations();

        builder.Services.AddDbContext<DataContext>();
        
        builder.Services.AddScoped(typeof(DatabaseRepository<>));
    }
}