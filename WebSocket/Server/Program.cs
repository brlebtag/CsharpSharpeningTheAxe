using System.Collections.Concurrent;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WebSocketConnectionManager>();

var app = builder.Build();

app.UseWebSockets();

var logger = app.Logger;

// WebSocket upgrade endpoint — assigns a UID to each connection and sends it back to the client
app.Map("/ws", async (HttpContext context, WebSocketConnectionManager manager) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        logger.LogError("Non-WebSocket request to /ws from {RemoteIp}", context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    logger.LogInformation("New WebSocket connected!");

    WebSocket? webSocket = null;
    var uid = Guid.NewGuid().ToString();

    try
    {
        webSocket = await context.WebSockets.AcceptWebSocketAsync();

        var uidBytes = System.Text.Encoding.UTF8.GetBytes(uid);
        await webSocket.SendAsync(uidBytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);

        manager.Add(uid, webSocket);

        logger.LogInformation("WebSocket registered as '{uid}'!", uid);

        var buffer = new byte[4096];
        while (webSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
            }
            catch (WebSocketException wsex)
            {
                logger.LogWarning(wsex, "WebSocket '{uid}' receive failed, removing connection", uid);
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error receiving from WebSocket '{uid}'", uid);
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close)
                break;
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to accept or handle WebSocket from {RemoteIp}", context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    }
    finally
    {
        manager.Remove(uid);

        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            try
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error while closing WebSocket '{uid}'", uid);
            }
        }
    }
});

// Send binary data to a specific WebSocket identified by UID
app.MapPost("/send/{uid}", async (string uid, HttpContext context, WebSocketConnectionManager manager) =>
{
    if (!manager.TryGet(uid.Trim(), out var webSocket) || webSocket is null)
    {
        logger.LogError("WebSocket '{uid}' not found when attempting to send", uid);
        return Results.NotFound($"WebSocket '{uid}' not found.");
    }

    if (webSocket.State != WebSocketState.Open)
    {
        logger.LogError("WebSocket '{uid}' is not open (state: {state})", uid, webSocket.State);
        return Results.BadRequest($"WebSocket '{uid}' is not open.");
    }

    using var ms = new MemoryStream();
    await context.Request.Body.CopyToAsync(ms);
    var data = ms.ToArray();

    try
    {
        await webSocket.SendAsync(data, WebSocketMessageType.Binary, endOfMessage: true, CancellationToken.None);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to send data to WebSocket '{uid}'", uid);
        return Results.StatusCode(500);
    }
});

app.Run();

class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    public void Add(string uid, WebSocket webSocket) => _connections[uid] = webSocket;

    public bool TryGet(string uid, out WebSocket? webSocket) => _connections.TryGetValue(uid, out webSocket);

    public void Remove(string uid) => _connections.TryRemove(uid, out _);
}
