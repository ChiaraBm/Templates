using WebAppTemplate.ApiServer.Startup;

var startup = new Startup();

await startup.InitializeAsync(args);

var builder = WebApplication.CreateBuilder(args);

await startup.AddWebAppTemplateAsync(builder);

var app = builder.Build();

await startup.AddWebAppTemplateAsync(app);

await app.RunAsync();