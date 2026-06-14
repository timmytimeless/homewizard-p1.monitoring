using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aiterate.energy.web.Services;

public class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<ClientWebSocket, string> _connections = new();

    public void Register(ClientWebSocket socket, string unsubscribeMessage)
    {
        if (socket == null) return;
        _connections.TryAdd(socket, unsubscribeMessage ?? string.Empty);
    }

    public void Unregister(ClientWebSocket socket)
    {
        if (socket == null) return;
        _connections.TryRemove(socket, out _);
    }

    public async Task CloseAllAsync()
    {
        var entries = _connections.ToArray();
        foreach (var kv in entries)
        {
            var ws = kv.Key;
            var unsub = kv.Value ?? string.Empty;
            try
            {
                if (ws.State == WebSocketState.Open)
                {
                    if (!string.IsNullOrEmpty(unsub))
                    {
                        var bytes = Encoding.UTF8.GetBytes(unsub);
                        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    }

                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "app-stopping", CancellationToken.None);
                }
            }
            catch
            {
                // ignore cleanup errors
            }
            finally
            {
                _connections.TryRemove(ws, out _);
            }
        }
    }
}
