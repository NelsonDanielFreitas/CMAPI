using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using CMAPI.Models;
using CMAPI.Services.Chat;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CMAPI.Middleware;

public class ChatHub
{
    private readonly WebSocket _socket;
    private readonly IChatService _chatService;
    private readonly ClaimsPrincipal _user;
    private readonly ILogger<ChatHub> _logger;
    private readonly WebSocketConnectionManager _connectionManager;
    private Guid _currentAvariaId;
    private Guid _userId;
    private const int MaxMessageSize = 4096;
    private const int ReceiveBufferSize = 4096;

    public ChatHub(
        WebSocket socket, 
        IChatService chatService, 
        ClaimsPrincipal user, 
        ILogger<ChatHub> logger,
        WebSocketConnectionManager connectionManager)
    {
        _socket = socket;
        _chatService = chatService;
        _user = user;
        _logger = logger;
        _connectionManager = connectionManager;
        _userId = Guid.Parse(user.FindFirst("Id")?.Value ?? throw new InvalidOperationException("User ID not found"));
    }

    public async Task ProcessAsync()
    {
        var buffer = new byte[ReceiveBufferSize];
        var receiveBuffer = new ArraySegment<byte>(buffer);
        var cancellationToken = CancellationToken.None;

        try
        {
            var result = await _socket.ReceiveAsync(receiveBuffer, cancellationToken);
            while (!result.CloseStatus.HasValue)
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var dto = JsonSerializer.Deserialize<IncomingChatDto>(json);
                        
                        if (dto == null || string.IsNullOrEmpty(dto.message))
                        {
                            await SendErrorAsync("Invalid message format");
                            continue;
                        }

                        // Handle connection to specific avaria
                        if (dto.avariaId != _currentAvariaId)
                        {
                            // If we were connected to a different avaria, remove that connection
                            if (_currentAvariaId != Guid.Empty)
                            {
                                await _connectionManager.RemoveConnection(_currentAvariaId, _userId);
                            }
                            
                            // Add connection to new avaria
                            _currentAvariaId = dto.avariaId;
                            _connectionManager.AddConnection(_currentAvariaId, _userId, _socket);
                            
                            // Send connection confirmation
                            await SendMessageAsync(new { type = "connected", avariaId = _currentAvariaId });
                        }

                        var incoming = new ChatMessage
                        {
                            AvariaId = dto.avariaId,
                            Message = dto.message,
                            SenderId = _userId
                        };

                        var saved = await _chatService.SaveMessageAsync(incoming);
                        
                        // Broadcast to all users connected to this avaria
                        var broadcastMessage = JsonSerializer.Serialize(new
                        {
                            type = "message",
                            message = saved
                        });
                        
                        await _connectionManager.BroadcastToAvaria(_currentAvariaId, broadcastMessage);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Error deserializing message");
                        await SendErrorAsync("Invalid message format");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message");
                        await SendErrorAsync("Error processing message");
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (_currentAvariaId != Guid.Empty)
                    {
                        await _connectionManager.RemoveConnection(_currentAvariaId, _userId);
                    }
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    break;
                }

                result = await _socket.ReceiveAsync(receiveBuffer, cancellationToken);
            }

            if (result.CloseStatus.HasValue)
            {
                if (_currentAvariaId != Guid.Empty)
                {
                    await _connectionManager.RemoveConnection(_currentAvariaId, _userId);
                }
                await _socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription ?? "Closing", cancellationToken);
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogError(ex, "WebSocket error occurred");
            if (_currentAvariaId != Guid.Empty)
            {
                await _connectionManager.RemoveConnection(_currentAvariaId, _userId);
            }
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Internal server error", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in WebSocket processing");
            if (_currentAvariaId != Guid.Empty)
            {
                await _connectionManager.RemoveConnection(_currentAvariaId, _userId);
            }
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Internal server error", cancellationToken);
            }
        }
    }

    private async Task SendMessageAsync(object message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(json);
            await _socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            throw;
        }
    }

    private async Task SendErrorAsync(string errorMessage)
    {
        try
        {
            var error = new { type = "error", error = errorMessage };
            var json = JsonSerializer.Serialize(error);
            var buffer = Encoding.UTF8.GetBytes(json);
            await _socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending error message");
        }
    }
    
    public class IncomingChatDto
    {
        public Guid avariaId { get; set; }
        public string message { get; set; }
    }
}