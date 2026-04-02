using Cline01.Models;

namespace Cline01.Services.Interfaces;

public interface IEvaluationJobQueue
{
    void Enqueue(EvaluationJob job);
    Task<EvaluationJob?> DequeueAsync(CancellationToken cancellationToken);
}
