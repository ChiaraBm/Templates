using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebAppTemplate.Frontend.Startup;

var startup = new Startup();
await startup.InitializeAsync();

var builder = WebAssemblyHostBuilder.CreateDefault(args);

await startup.AddWebAppTemplateAsync(builder);

var app = builder.Build();

await startup.AddWebAppTemplateAsync(app);

await app.RunAsync();