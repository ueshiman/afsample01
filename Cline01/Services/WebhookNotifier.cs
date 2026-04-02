using Cline01.Models;
using Cline01.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;

namespace Cline01.Services;

public class WebhookNotifier : IWebhookNotifier
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookNotifier> _logger;
    private readonly WebhookSettings _settings;

    public WebhookNotifier(
        IHttpClientFactory httpClientFactory,
        IOptions<WebhookSettings> settings,
        ILogger<WebhookNotifier> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Webhook");
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task NotifyAsync(EvaluationWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Url))
        {
            _logger.LogWarning("Webhook URL not configured. Skipping notification for job {JobId}", payload.JobId);
            return;
        }

        _logger.LogInformation("Sending webhook notification for job {JobId} to {Url}", payload.JobId, _settings.Url);

        try
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_settings.Url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook notification sent successfully for job {JobId}", payload.JobId);
            }
            else
            {
                _logger.LogWarning("Webhook notification failed for job {JobId}. Status: {Status}", 
                    payload.JobId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending webhook notification for job {JobId}", payload.JobId);
        }
    }
}

public class WebhookSettings
{
    public string Url { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
