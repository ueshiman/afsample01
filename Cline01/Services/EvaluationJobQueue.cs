using Cline01.Models;
using Cline01.Services.Interfaces;
using System.Threading.Channels;

namespace Cline01.Services;

public class EvaluationJobQueue : IEvaluationJobQueue
{
    private readonly Channel<EvaluationJob> _queue;
    private readonly ILogger<EvaluationJobQueue> _logger;

    public EvaluationJobQueue(ILogger<EvaluationJobQueue> logger)
    {
        _logger = logger;
        _queue = Channel.CreateUnbounded<EvaluationJob>();
    }

    public void Enqueue(EvaluationJob job)
    {
        if (_queue.Writer.TryWrite(job))
        {
            _logger.LogInformation("Job {JobId} enqueued successfully", job.JobId);
        }
        else
        {
            _logger.LogError("Failed to enqueue job {JobId}", job.JobId);
            throw new InvalidOperationException($"Failed to enqueue job {job.JobId}");
        }
    }

    public async Task<EvaluationJob?> DequeueAsync(CancellationToken cancellationToken)
    {
        try
        {
            var job = await _queue.Reader.ReadAsync(cancellationToken);
            _logger.LogInformation("Job {JobId} dequeued", job.JobId);
            return job;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Dequeue operation cancelled");
            return null;
        }
    }
}
