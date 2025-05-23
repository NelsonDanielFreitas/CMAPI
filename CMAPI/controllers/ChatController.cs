using CMAPI.Services.Chat;
using Microsoft.AspNetCore.Mvc;

namespace CMAPI.controllers;
[Route("api/chat")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpGet("GetChatHistory/{avariaId}")]
    public async Task<IActionResult> GetChatHistory(Guid avariaId)
    {
        var history = await _chatService.GetHistoryAsync(avariaId);
        return Ok(history);
    }
    
}
