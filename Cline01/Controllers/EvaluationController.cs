using Cline01.Models;
using Cline01.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cline01.Controllers;

[ApiController]
[Route("api/messages")]
public class EvaluationController : ControllerBase
{
    private readonly IEvaluationJobQueue _jobQueue;
    private readonly ILogger<EvaluationController> _logger;

    public EvaluationController(
        IEvaluationJobQueue jobQueue,
        ILogger<EvaluationController> logger)
    {
        _jobQueue = jobQueue;
        _logger = logger;
    }

    [HttpPost("evaluate")]
    public IActionResult EvaluateMessage([FromBody] EvaluateMessageRequest request)
    {
        _logger.LogInformation("Received evaluation request for conversation {ConversationId}, message {MessageId}",
            request.ConversationId, request.MessageId);

        // Validate request
        if (string.IsNullOrWhiteSpace(request.ConversationId) ||
            string.IsNullOrWhiteSpace(request.MessageId) ||
            string.IsNullOrWhiteSpace(request.SentenceText) ||
            string.IsNullOrWhiteSpace(request.SpeakerRole))
        {
            _logger.LogWarning("Invalid request: missing required fields");
            return BadRequest(new { error = "Missing required fields" });
        }

        // Generate job ID
        var jobId = $"job-{Guid.NewGuid():N}";

        // Create evaluation job
        var job = new EvaluationJob
        {
            JobId = jobId,
            ConversationId = request.ConversationId,
            MessageId = request.MessageId,
            SentenceText = request.SentenceText,
            SpeakerRole = request.SpeakerRole,
            Timestamp = request.Timestamp,
            EnqueuedAt = DateTime.UtcNow
        };

        // Enqueue job
        try
        {
            _jobQueue.Enqueue(job);
            
            _logger.LogInformation("Job {JobId} enqueued for conversation {ConversationId}",
                jobId, request.ConversationId);

            return Ok(new EvaluateMessageResponse
            {
                Accepted = true,
                JobId = jobId,
                Status = "Accepted"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue job");
            return StatusCode(500, new { error = "Failed to enqueue evaluation job" });
        }
    }
}
