using MMOServer.Models;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace MMOServer.Network;

public class ConnectionManager
{

    private readonly ConcurrentDictionary<int, Player> _onlinePlayers = new ConcurrentDictionary<int, Player>();

    public ChunkManager Chunks { get; } = new ChunkManager();

    public void AddPlayer(Player player)
    {

        if (_onlinePlayers.TryAdd(player.Id, player))
        {
            var (cx, cy) = Chunks.GetChunkCoords(player.Position.X, player.Position.Y);

            player.ChunkX = cx;
            player.ChunkY = cy;

            Chunk currentChunk = Chunks.GetChunk(cx, cy);

            if (currentChunk != null)
            {
                currentChunk.PlayersInside.Add(player.Id);
            }

            Console.WriteLine($"[Сервер] Игрок {player.Name} зашел в чанк [{cx},{cy}]. Всего онлайн: {_onlinePlayers.Count}");
        }
    }


    public void RemovePlayer(int playerId)
    {
        if (_onlinePlayers.TryRemove(playerId, out Player removedPlayer))
        {
            Chunk currentChunk = Chunks.GetChunk(removedPlayer.ChunkX, removedPlayer.ChunkY);

            if (currentChunk != null)
            {
                currentChunk.PlayersInside.Remove(removedPlayer.Id);    
            }

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

    public async Task BroadcastToVisibleAsync(int tileX, int tileY, object payload, int? excludePlayerId = null)
    {
        var visibleChunks = Chunks.GetVisibleChunks(tileX, tileY);
        await BroadcastToChunksAsync(visibleChunks, payload, excludePlayerId);
    }

    public async Task BroadcastToChunksAsync(IEnumerable<Chunk> chunks, object payload, int? excludePlayerId = null)
    {
        foreach (Chunk chunk in chunks)
        {
            foreach (int playerId in chunk.PlayersInside)
            {
                if (playerId == excludePlayerId) continue;

                if (_onlinePlayers.TryGetValue(playerId, out Player p) && p.ClientSocket.State == WebSocketState.Open)
                {
                    await PacketUtility.SendAsync(p.ClientSocket, payload);
                }
            }
        }
    }

    public async Task UpdatePlayerPosition(Player player, Position newPos)
    {
        Position oldPos = player.Position;
        player.Position = newPos;

        var (oldCx, oldCy) = (player.ChunkX, player.ChunkY);
        var (newCx, newCy) = Chunks.GetChunkCoords(newPos.X, newPos.Y);

        if (oldCx != newCx || oldCy != newCy)
        {
            Chunks.GetChunk(oldCx, oldCy)?.PlayersInside.Remove(player.Id);
            player.ChunkX = newCx;
            player.ChunkY = newCy;
            Chunks.GetChunk(newCx, newCy)?.PlayersInside.Add(player.Id);
        }

        var oldChunks = Chunks.GetVisibleChunks(oldPos.X, oldPos.Y).ToHashSet();
        var newChunks = Chunks.GetVisibleChunks(newPos.X, newPos.Y).ToHashSet();

        var chunksLeft = oldChunks.Except(newChunks).ToList();
        var chunksEntered = newChunks.Except(oldChunks).ToList();
        var chunksStayed = oldChunks.Intersect(newChunks).ToList();

        // А) Для тех, кто видел нас ДО и ПОСЛЕ шага -> шлем обычный player_moved
        var moveEvent = new { @event = "player_moved", id = player.Id, x = newPos.X, y = newPos.Y };
        await BroadcastToChunksAsync(chunksStayed, moveEvent);

        // Б) Для тех, кто остался ПОЗАДИ -> шлем им, что мы исчезли (player_left)
        var leftEvent = new { @event = "player_left", id = player.Id };
        await BroadcastToChunksAsync(chunksLeft, leftEvent, player.Id);

        foreach (var chunk in chunksLeft)
        {
            foreach (var pId in chunk.PlayersInside)
            {
                await PacketUtility.SendAsync(player.ClientSocket, new { @event = "player_left", id = pId });
            }
        }

        var joinedEvent = new { @event = "player_joined", id = player.Id, x = newPos.X, y = newPos.Y };
        await BroadcastToChunksAsync(chunksEntered, joinedEvent, player.Id);
        foreach (var chunk in chunksEntered)
        {
            foreach (var pId in chunk.PlayersInside)
            {
                if (pId == player.Id) continue;
                if (_onlinePlayers.TryGetValue(pId, out Player other))
                {
                    await PacketUtility.SendAsync(player.ClientSocket, new { @event = "player_joined", id = other.Id, x = other.Position.X, y = other.Position.Y });
                }
            }
        }
    }
}
