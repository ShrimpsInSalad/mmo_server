using MMOServer.Models;
using System;
using System.Collections.Concurrent;

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






    public ConnectionManager()
	{
	}
}
