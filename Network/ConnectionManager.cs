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
        BroadcastToChunks(visibleChunks, payload, excludePlayerId);
    }

    public void BroadcastToChunks(IEnumerable<Chunk> chunks, object payload, int? excludePlayerId = null)
    {
        foreach (Chunk chunk in chunks)
        {
            foreach (int playerId in chunk.PlayersInside)
            {
                if (playerId == excludePlayerId) continue;

                if (_onlinePlayers.TryGetValue(playerId, out Player p) && p.ClientSocket.State == WebSocketState.Open)
                {
                    PacketUtility.EnqueuePacket(p, payload);
                }
            }
        }
    }

    public  void UpdatePlayerPosition(Player player, Position newPos, MobManager mobManager)
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

        var chunksLeft = new List<Chunk>();
        var chunksEntered = new List<Chunk>();
        var chunksStayed = new List<Chunk>();

        foreach (var chunk in oldChunks)
        {
            if (newChunks.Contains(chunk))
            {
                chunksStayed.Add(chunk);
            }
            else
            {
                chunksLeft.Add(chunk);
            }
        }

        foreach (var chunk in newChunks)
        {
            if (!oldChunks.Contains(chunk))
            {
                chunksEntered.Add(chunk);
            }
        }

        // А) Для тех, кто видел нас ДО и ПОСЛЕ шага -> шлем обычный player_moved
        var moveEvent = new { @event = "player_moved", id = player.Id, x = newPos.X, y = newPos.Y };
        BroadcastToChunks(chunksStayed, moveEvent);

        // Б) Для тех, кто остался ПОЗАДИ -> шлем им, что мы исчезли (player_left)
        var leftEvent = new { @event = "player_left", id = player.Id };
        BroadcastToChunks(chunksLeft, leftEvent, player.Id);

        foreach (var chunk in chunksLeft)
        {
            foreach (var pId in chunk.PlayersInside)
            {
                PacketUtility.EnqueuePacket(player, new { @event = "player_left", id = pId });
            }
        }

        var joinedEvent = new { @event = "player_joined", id = player.Id, x = newPos.X, y = newPos.Y };

        BroadcastToChunks(chunksEntered, joinedEvent, player.Id);

        foreach (var chunk in chunksEntered)
        {
            foreach (var pId in chunk.PlayersInside)
            {
                if (pId == player.Id) continue;
                if (_onlinePlayers.TryGetValue(pId, out Player other))
                {
                    PacketUtility.EnqueuePacket(player, new { @event = "player_joined", id = other.Id, x = other.Position.X, y = other.Position.Y });
                }
            }
        }

        SendMobsInChunksToPlayer(player, chunksEntered, mobManager);
    }

    // Метод точечной отправки мобов одному игроку (когда он открывает новую зону)
    public void SendMobsInChunksToPlayer(Player player, IEnumerable<Chunk> chunks, MobManager mobManager)
    {
        foreach (var chunk in chunks)
        {
            foreach (var mobId in chunk.MobsInside)
            {
                var mob = mobManager.GetMob(mobId);
                if (mob != null)
                {
                    var mobJoinedEvent = new
                    {
                        @event = "mob_joined",
                        id = mob.Id,
                        template_id = mob.TemplateId,
                        hp = mob.CurrentHp,
                        x = mob.Position.X,
                        y = mob.Position.Y
                    };
                    PacketUtility.EnqueuePacket(player, mobJoinedEvent);
                }
            }
        }
    }
}
