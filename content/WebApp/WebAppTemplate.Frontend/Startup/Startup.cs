using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace WebAppTemplate.Frontend.Startup;

public partial class Startup
{
    // WebAssemblyHost
    public WebAssemblyHostBuilder WebAssemblyHostBuilder { get; private set; }
    public WebAssemblyHost WebAssemblyHost { get; private set; }
    
    public Task Initialize()
    {
        return Task.CompletedTask;
    }
    
    public async Task AddWebAppTemplate(WebAssemblyHostBuilder builder)
    {
        WebAssemblyHostBuilder = builder;
        
        await RegisterLogging();
        await RegisterBase();
        await RegisterAuthentication();
        
    }
    
    public Task AddWebAppTemplate(WebAssemblyHost assemblyHost)
    {
        return Task.CompletedTask;
    }
}