using System.Collections.Concurrent;
using System.Net.WebSockets;
using CMAPI.Models;

namespace CMAPI.Services.Chat;

public class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> _avariaConnections = new();
    private readonly ILogger<WebSocketConnectionManager> _logger;

    public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
    {
        _logger = logger;
    }

    public string AddConnection(Guid avariaId, Guid userId, WebSocket socket)
    {
        var avariaKey = avariaId.ToString();
        var userKey = userId.ToString();

        var connections = _avariaConnections.GetOrAdd(avariaKey, _ => new ConcurrentDictionary<string, WebSocket>());
        connections.TryAdd(userKey, socket);

        _logger.LogInformation("User {UserId} connected to avaria {AvariaId}", userId, avariaId);
        return userKey;
    }

    public async Task RemoveConnection(Guid avariaId, Guid userId)
    {
        var avariaKey = avariaId.ToString();
        var userKey = userId.ToString();

        if (_avariaConnections.TryGetValue(avariaKey, out var connections))
        {
            if (connections.TryRemove(userKey, out var socket))
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection removed", CancellationToken.None);
                }
                _logger.LogInformation("User {UserId} disconnected from avaria {AvariaId}", userId, avariaId);
            }
        }
    }

    public async Task BroadcastToAvaria(Guid avariaId, string message)
    {
        var avariaKey = avariaId.ToString();
        if (_avariaConnections.TryGetValue(avariaKey, out var connections))
        {
            var tasks = connections.Select(async kvp =>
            {
                if (kvp.Value.State == WebSocketState.Open)
                {
                    try
                    {
                        var buffer = System.Text.Encoding.UTF8.GetBytes(message);
                        await kvp.Value.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error broadcasting message to user {UserId} in avaria {AvariaId}", kvp.Key, avariaId);
                    }
                }
            });

            await Task.WhenAll(tasks);
        }
    }

    public bool IsUserConnectedToAvaria(Guid avariaId, Guid userId)
    {
        var avariaKey = avariaId.ToString();
        var userKey = userId.ToString();

        return _avariaConnections.TryGetValue(avariaKey, out var connections) &&
               connections.ContainsKey(userKey);
    }
} 