using WebAppTemplate.ApiServer.Startup;

var startup = new Startup();

await startup.Initialize(args);

var builder = WebApplication.CreateBuilder(args);

await startup.AddWebAppTemplate(builder);

var app = builder.Build();

await startup.AddWebAppTemplate(app);

await app.RunAsync();