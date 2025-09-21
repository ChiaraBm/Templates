using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace WebAppTemplate.Frontend.Startup;

public partial class Startup
{
    // WebAssemblyHost
    public WebAssemblyHostBuilder WebAssemblyHostBuilder { get; private set; }
    public WebAssemblyHost WebAssemblyHost { get; private set; }
    
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
    
    public async Task AddWebAppTemplateAsync(WebAssemblyHostBuilder builder)
    {
        WebAssemblyHostBuilder = builder;
        
        await RegisterLoggingAsync();
        await RegisterBaseAsync();
        await RegisterAuthenticationAsync();
        
    }
    
    public Task AddWebAppTemplateAsync(WebAssemblyHost assemblyHost)
    {
        return Task.CompletedTask;
    }
}