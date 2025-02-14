using Microsoft.EntityFrameworkCore;
using WebAppTemplate.ApiServer.Configuration;
using WebAppTemplate.ApiServer.Database.Entities;

namespace WebAppTemplate.ApiServer.Database;

public class DataContext : DbContext
{
    public DbSet<User> Users { get; set; }

    private readonly AppConfiguration Configuration;

    public DataContext(AppConfiguration configuration)
    {
        Configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;
        
        var config = Configuration.Database;

        var connectionString = $"Host={config.Host};" +
                               $"Port={config.Port};" +
                               $"Database={config.Database};" +
                               $"Username={config.Username};" +
                               $"Password={config.Password}";
        
        optionsBuilder.UseNpgsql(
            connectionString,
            builder =>
            {
                builder.EnableRetryOnFailure(5);
                builder.MigrationsHistoryTable("MigrationHistory");
            }
        );
    }
}