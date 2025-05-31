using CMAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMAPI.controllers;

[Route("api/ollama")]
[ApiController]
public class OllamaController : ControllerBase
{
    private readonly OllamaService _ollamaService;

    public OllamaController(OllamaService ollamaService)
    {
        _ollamaService = ollamaService;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { message = "Message cannot be empty" });

            var response = await _ollamaService.GetLLMResponseAsync(request.Message);
            return Ok(new { response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error processing request", error = ex.Message });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
} 