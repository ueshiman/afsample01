using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tutorial01B_WinUIClient;

public sealed class CallbackServer : IAsyncDisposable
{
    private WebApplication? _application;

    public async Task StartAsync(
        Func<ExecuteCallbackPayload, bool> callbackHandler,
        CancellationToken cancellationToken = default)
    {
        if (_application is not null)
        {
            return;
        }

        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:1305");
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.PropertyNameCaseInsensitive = true);

        var application = builder.Build();
        application.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        application.MapPost(
            "/callback",
            (ExecuteCallbackPayload payload) =>
                callbackHandler(payload)
                    ? Results.NoContent()
                    : Results.NotFound(new { error = "Unknown or expired requestId." }));

        await application.StartAsync(cancellationToken);
        _application = application;
    }

    public async ValueTask DisposeAsync()
    {
        if (_application is null)
        {
            return;
        }

        await _application.StopAsync();
        await _application.DisposeAsync();
        _application = null;
    }
}
