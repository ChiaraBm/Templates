using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using SharpWebview;
using SharpWebview.Content;

namespace DesktopApp.Services;

public class WebViewService : IHostedLifecycleService
{
    private Webview Window;

    private readonly IServer Server;
    private readonly ILogger<WebViewService> Logger;
    private readonly IHost Host;

    public WebViewService(IServer server, ILogger<WebViewService> logger, IHost host)
    {
        Server = server;
        Logger = logger;
        Host = host;
    }

    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting webview");
        
        // Determine server address so we know where to start the view
        var serverAddressesFeature = Server.Features.Get<IServerAddressesFeature>();

        if (serverAddressesFeature == null)
        {
            Logger.LogWarning("Unable to start webview. Unable to load ServerAddressesFeature");
            return;
        }

        var address = serverAddressesFeature.Addresses.First();

        // We run it in a new task, because the Run() method is blocking
        Task.Run(async () =>
        {
            try
            {
                Window = new Webview();

                Window.SetTitle("DesktopApp");
                Window.SetSize(1024, 768, WebviewHint.None);
                Window.SetSize(800, 600, WebviewHint.Min);
                Window.Navigate(new UrlContent(address));
                Window.Run();

                // If we reach this part of the code, stop the web app too
                await Host.StopAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                Logger.LogCritical("Unable to start webview: {e}", e);
            }
        });
    }

    public Task StartingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Destroying webview");
        
        Window.Dispatch(() =>
        {
            Window.Dispose();
        });
        
        return Task.CompletedTask;
    }
}