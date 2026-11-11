using MMOServer.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMOServer.Network;

public class MobManager
{

    private readonly ConcurrentDictionary<int, Mob> _activeMobs = new();

    private readonly ChunkManager _chunkManager;

    public MobManager(ChunkManager chunkManager)
    {
        _chunkManager = chunkManager;
    }

    public void AddMob(Mob mob)
    {
        if (_activeMobs.TryAdd(mob.Id, mob))
        {
            var (cx, cy) = _chunkManager.GetChunkCoords(mob.Position.X, mob.Position.Y);
            mob.ChunkX = cx;
            mob.ChunkY = cy;

            Chunk chunk = _chunkManager.GetChunk(cx, cy);
            if (chunk != null)
            {
                chunk.MobsInside.Add(mob.Id);
            }
        }
    }
    public void RemoveMob(int mobId)
    {
        if (_activeMobs.TryRemove(mobId, out Mob removedMob))
        {
            Chunk chunk = _chunkManager.GetChunk(removedMob.ChunkX, removedMob.ChunkY);
            if (chunk != null)
            {
                chunk.MobsInside.Remove(mobId);
            }
        }
    }

    public void UpdateActiveMobs(ulong currentServerTick, IEnumerable<Player> onlinePlayers)
    {
        int targetSlot = (int)(currentServerTick % 20);

        HashSet<Chunk> activeChunks = new HashSet<Chunk>();

        foreach (var player in onlinePlayers)
        {
            var visible = _chunkManager.GetVisibleChunks(player.Position.X, player.Position.Y);

            foreach (var chunk in visible)
            {
                activeChunks.Add(chunk);
            }
        }

        foreach (var chunk in activeChunks)
        {
            foreach (int mobId in chunk.MobsInside)
            {
                if (_activeMobs.TryGetValue(mobId, out Mob mob))
                {
                    if (mob.MobSlot == targetSlot)
                    {
                         mob.Brain?.Update(mob, currentServerTick);
                    }
                }
            }
        }
    }

    public Mob GetMob(int mobId)
    {
        _activeMobs.TryGetValue(mobId, out Mob mob);
        return mob;
    }

    public IEnumerable<Mob> GetAllMobs() => _activeMobs.Values;

    public void UpdateMobPosition(Mob mob, Position newPos, ConnectionManager connManager)
    {
        Position oldPos = mob.Position;
        mob.Position = newPos;

        // 1. ПРОВЕРКА ПЕРЕХОДА ГРАНИЦЫ ЧАНКА
        var (oldCx, oldCy) = (mob.ChunkX, mob.ChunkY);
        var (newCx, newCy) = _chunkManager.GetChunkCoords(newPos.X, newPos.Y);

        if (oldCx != newCx || oldCy != newCy)
        {
            _chunkManager.GetChunk(oldCx, oldCy)?.MobsInside.Remove(mob.Id);
            mob.ChunkX = newCx;
            mob.ChunkY = newCy;
            _chunkManager.GetChunk(newCx, newCy)?.MobsInside.Add(mob.Id);
        }

        // 2. МАТЕМАТИКА ВИДИМОСТИ (Та же самая!)
        var oldChunks = _chunkManager.GetVisibleChunks(oldPos.X, oldPos.Y).ToHashSet();
        var newChunks = _chunkManager.GetVisibleChunks(newPos.X, newPos.Y).ToHashSet();

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

        // А) Оповещаем тех, кто видел моба до и после шага
        var moveEvent = new { @event = "mob_moved", id = mob.Id, x = newPos.X, y = newPos.Y };
        connManager.BroadcastToChunks(chunksStayed, moveEvent);

        // Б) Оповещаем тех, у кого моб пропал из зоны видимости
        var leftEvent = new { @event = "mob_left", id = mob.Id };
        connManager.BroadcastToChunks(chunksLeft, leftEvent);

        // В) Оповещаем тех, у кого моб только что появился из тумана войны
        var joinedEvent = new
        {
            @event = "mob_joined",
            id = mob.Id,
            template_id = mob.TemplateId,
            hp = mob.CurrentHp,
            x = newPos.X,
            y = newPos.Y
        };
        connManager.BroadcastToChunks(chunksEntered, joinedEvent);
    }
}

