using MoonCore.Configuration;
using MoonCore.Extended.Extensions;
using MoonCore.Extensions;
using MoonCore.Helpers;
using MoonCore.Services;
using WebApp.ApiServer;
using WebApp.ApiServer.Configuration;
using WebApp.ApiServer.Http.Middleware;

Directory.CreateDirectory(PathBuilder.Dir("storage"));

// Setup configuration
var configurationOptions = new ConfigurationOptions();
var configurationService = new ConfigurationService();

configurationOptions.AddConfiguration<AppConfiguration>("app");
configurationOptions.Path = PathBuilder.Dir("storage");
configurationOptions.EnvironmentPrefix = "WebApp".ToUpper();

// Configure startup logger
var startupLoggerFactory = new LoggerFactory();

startupLoggerFactory.AddMoonCore(configuration =>
{
    configuration.Console.Enable = true;
    configuration.Console.EnableAnsiMode = true;
    configuration.FileLogging.Enable = false;
});

var startupLogger = startupLoggerFactory.CreateLogger("Startup");

// Load app configuration
var config = configurationService.GetConfiguration<AppConfiguration>(configurationOptions, startupLogger);

var builder = WebApplication.CreateBuilder(args);

// Setup logging
await Startup.ConfigureLogging(builder);

// Configure configuration service
configurationService.RegisterInDi(configurationOptions, builder.Services);
builder.Services.AddSingleton(configurationService);

// Scan assembly for services
builder.Services.AutoAddServices<Program>();

// Configure database
await Startup.ConfigureDatabase(builder, startupLoggerFactory);

// Add default services
builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

await Startup.ConfigureOAuth2(builder, startupLogger, config);

var app = builder.Build();

await Startup.PrepareDatabase(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseApiErrorHandling();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

await Startup.UseOAuth2(app, config);

app.UseMiddleware<AuthorizationMiddleware>();

app.MapControllers();
app.MapFallbackToFile("index.html");

await app.RunAsync();