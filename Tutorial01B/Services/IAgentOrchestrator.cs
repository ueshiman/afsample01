using Microsoft.Graph;

namespace Tutorial01B.Services;

public interface IAgentOrchestrator
{
    Task<Guid> ExecuteAsync(Uri callback, string input, Guid? sessionId, CancellationToken cancellationToken = default);

    Task<Guid> HandleAsync(Uri callback, string message, Guid? sessionId, CancellationToken cancellationToken = default);
}