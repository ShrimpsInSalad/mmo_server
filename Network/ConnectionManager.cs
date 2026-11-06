using MMOServer.Models;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace MMOServer.Network;

public class ConnectionManager
{

    private readonly ConcurrentDictionary<int, Player> _onlinePlayers = new ConcurrentDictionary<int, Player>();


    public void AddPlayer(Player player)
    {
        bool isAdded = _onlinePlayers.TryAdd(player.Id, player);

        if (isAdded)
        {
            Console.WriteLine($"[Сервер] Игрок {player.Name} подключился. Всего онлайн: {_onlinePlayers.Count}");
        }

    }


    public void RemovePlayer(int playerId)
    {
        bool isRemoved = _onlinePlayers.TryRemove(playerId, out Player removedPlayer);

        if (isRemoved)
        {
            Console.WriteLine($"[Сервер] Игрок {removedPlayer.Name} отключился. Всего онлайн: {_onlinePlayers.Count}");
        }
    }

    public Player GetPlayer(int playerId)
    {
        _onlinePlayers.TryGetValue(playerId, out Player foundPlayer);

        return foundPlayer;
    }

    public IEnumerable<Player> GetAllPlayers()
    {
        return _onlinePlayers.Values;
    }

    public async Task BroadcastAsync(string jsonMessage)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(jsonMessage);
        var buffer = new ArraySegment<byte>(bytes);

        foreach (var player in _onlinePlayers.Values)
        {
            if (player.ClientSocket.State == WebSocketState.Open)
            {
                await player.ClientSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
