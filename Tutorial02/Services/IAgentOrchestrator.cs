using Microsoft.Graph;

namespace Tutorial02.Services;

public interface IAgentOrchestrator
{
    Task<Guid> ExecuteAsync(Uri callback, string requestId, string input, Guid? sessionId, CancellationToken cancellationToken = default);

    Task<Guid> HandleAsync(Uri callback, string requestId, string message, Guid? sessionId, CancellationToken cancellationToken = default);
}