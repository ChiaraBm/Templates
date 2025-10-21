using WebAppTemplate.ApiServer.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.AddWebAppTemplate();

var app = builder.Build();

app.UseWebAppTemplate();

await app.RunAsync();