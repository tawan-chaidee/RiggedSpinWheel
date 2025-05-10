using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class WebSocketService
{
    private readonly ConcurrentDictionary<WebSocket, byte> _sockets = new ConcurrentDictionary<WebSocket, byte>();
    private Timer _timer;

    public async Task StartAsync(string url)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        Console.WriteLine($"WebSocket server listening on {url}...");

        // Setup timer to send messages every 2 seconds
        _timer = new Timer(SendMessagesToAll, null, 0, 2000);

        while (true)
        {
            var context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                await ProcessWebSocketRequest(context);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    private async Task ProcessWebSocketRequest(HttpListenerContext context)
    {
        WebSocket webSocket = null;
        try
        {
            var wsContext = await context.AcceptWebSocketAsync(null);
            webSocket = wsContext.WebSocket;
            _sockets.TryAdd(webSocket, 0);

            Console.WriteLine($"New client connected. Total clients: {_sockets.Count}");

            var buffer = new byte[1024];
            while (webSocket.State == WebSocketState.Open)
            {
                await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            if (webSocket != null)
            {
                _sockets.TryRemove(webSocket, out _);
                webSocket.Dispose();
                Console.WriteLine($"Client disconnected. Total clients: {_sockets.Count}");
            }
        }
    }

    private async void SendMessagesToAll(object state)
    {
        var message = $"Server time: {DateTime.Now:HH:mm:ss}";
        var buffer = Encoding.UTF8.GetBytes(message);

        foreach (var socket in _sockets.Keys)
        {
            try
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(
                        new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
                else
                {
                    _sockets.TryRemove(socket, out _);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error: {ex.Message}");
                _sockets.TryRemove(socket, out _);
            }
        }
    }

}
