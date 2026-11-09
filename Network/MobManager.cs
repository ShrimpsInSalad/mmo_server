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
    }
}

