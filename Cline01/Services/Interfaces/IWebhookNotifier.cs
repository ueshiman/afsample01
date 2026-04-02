using Cline01.Models;

namespace Cline01.Services.Interfaces;

public interface IWebhookNotifier
{
    Task NotifyAsync(EvaluationWebhookPayload payload, CancellationToken cancellationToken = default);
}
