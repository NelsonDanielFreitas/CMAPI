using CMAPI.Services.Chat;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace CMAPI.Middleware;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WebSocketMiddleware> _logger;
    private readonly ILogger<ChatHub> _chatHubLogger;
    private readonly WebSocketConnectionManager _connectionManager;

    public WebSocketMiddleware(
        RequestDelegate next, 
        ILogger<WebSocketMiddleware> logger, 
        ILogger<ChatHub> chatHubLogger,
        WebSocketConnectionManager connectionManager)
    {
        _next = next;
        _logger = logger;
        _chatHubLogger = chatHubLogger;
        _connectionManager = connectionManager;
    }

    public async Task InvokeAsync(HttpContext context, IChatService chatService)
    {
        if (context.Request.Path == "/ws/chat" && context.WebSockets.IsWebSocketRequest)
        {
            try
            {
                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                _logger.LogInformation("WebSocket connection established for user {UserId}", 
                    context.User.FindFirst("Id")?.Value ?? "unknown");
                
                var handler = new ChatHub(socket, chatService, context.User, _chatHubLogger, _connectionManager);
                await handler.ProcessAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket connection");
                if (context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            }
            return;
        }
        await _next(context);
    }
}