using Microsoft.AspNetCore.Mvc;
using Tutorial01B.Services;

namespace Tutorial01B.Controllers;

[ApiController]
[Route("api/orchestrator")]
public sealed class OrchestratorController : ControllerBase
{
    private readonly AgentOrchestrator _orchestrator;

    public OrchestratorController(AgentOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost("execute")]
    public IActionResult Execute([FromBody] ExecuteOrchestratorRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Input))
        {
            return BadRequest(new { error = "input is required." });
        }

        try
        {
            var result = _orchestrator
                .ExecuteAsync(request.Input, request.SessionId, HttpContext.RequestAborted)
                .GetAwaiter()
                .GetResult();

            var response = new ExecuteOrchestratorResponse
            {
                Input = request.Input,
                SessionId = request.SessionId,
                Results = result
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "orchestrator execution failed.", detail = ex.Message });
        }
    }

    public sealed class ExecuteOrchestratorRequest
    {
        public string Input { get; set; } = string.Empty;

        public string? SessionId { get; set; }
    }

    public sealed class ExecuteOrchestratorResponse
    {
        public string Input { get; set; } = string.Empty;

        public string? SessionId { get; set; }

        public IReadOnlyDictionary<string, string> Results { get; set; } = new Dictionary<string, string>();
    }
}
