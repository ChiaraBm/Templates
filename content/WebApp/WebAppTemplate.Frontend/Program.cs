using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebAppTemplate.Frontend.Startup;

var startup = new Startup();
await startup.Initialize();

var builder = WebAssemblyHostBuilder.CreateDefault(args);

await startup.AddWebAppTemplate(builder);

var app = builder.Build();

await startup.AddWebAppTemplate(app);

await app.RunAsync();