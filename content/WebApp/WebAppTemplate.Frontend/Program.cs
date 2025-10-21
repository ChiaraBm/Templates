using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebAppTemplate.Frontend.Startup;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.AddWebAppTemplate();

var app = builder.Build();

await app.RunAsync();