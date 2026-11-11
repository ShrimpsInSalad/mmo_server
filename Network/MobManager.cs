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

    private readonly ConnectionManager _connManager;
    private readonly ChunkManager _chunkManager;

    public MobManager(ConnectionManager connManager)
    {
        _connManager = connManager;
        _chunkManager = connManager.Chunks;
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
                lock (chunk.ChunkLock) { chunk.MobsInside.Add(mob.Id); }
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
                lock (chunk.ChunkLock) { chunk.MobsInside.Remove(mobId); }
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
                         mob.Brain?.Update(mob, currentServerTick, _connManager, this);
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
            var oldChunk = _chunkManager.GetChunk(oldCx, oldCy);
            if (oldChunk != null) lock (oldChunk.ChunkLock) { oldChunk.MobsInside.Remove(mob.Id); }

            mob.ChunkX = newCx;
            mob.ChunkY = newCy;

            var newChunk = _chunkManager.GetChunk(newCx, newCy);
            if (newChunk != null) lock (newChunk.ChunkLock) { newChunk.MobsInside.Add(mob.Id); }
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

    public void CheckAggroForPlayer(Player player, IEnumerable<Chunk> visibleChunks)
    {
        foreach (var chunk in visibleChunks)
        {
            foreach (var mobId in chunk.MobsInside)
            {
                if (_activeMobs.TryGetValue(mobId, out Mob mob))
                {
                    // Проверяем только свободных мобов (которые еще ни с кем не дерутся)
                    if (mob.CurrentTargetId == null)
                    {
                        var template = MobDatabase.GetTemplate(mob.TemplateId);

                        // Если моб агрессивный, проверяем дистанцию
                        if (template != null && template.Temperament == "Aggressive")
                        {
                            int dist = Math.Max(Math.Abs(mob.Position.X - player.Position.X), Math.Abs(mob.Position.Y - player.Position.Y));

                            // Если игрок подошел слишком близко — моб его замечает
                            if (dist <= template.VisionRadius)
                            {
                                mob.CurrentTargetId = player.Id;
                            }
                        }
                    }
                }
            }
        }
    }

    public void DamageMob(int mobId, int damage, int attackerId, ConnectionManager connManager)
    {
        if (_activeMobs.TryGetValue(mobId, out Mob mob))
        {
            var template = MobDatabase.GetTemplate(mob.TemplateId);
            if (template == null) return;

            // Наносим урон
            mob.CurrentHp -= damage;

            // 1. Моб запоминает обидчика (Это активирует Нейтралов и Трусов в UniversalBrain)
            mob.CurrentTargetId = attackerId;

            // 2. СТАЙНАЯ ЛОГИКА (Групповой агр по тегу)
            if (template.IsPackMob && !string.IsNullOrEmpty(template.GroupTag))
            {
                var visibleChunks = _chunkManager.GetVisibleChunks(mob.Position.X, mob.Position.Y);
                foreach (var chunk in visibleChunks)
                {
                    foreach (var friendId in chunk.MobsInside)
                    {
                        // Не проверяем самого себя
                        if (friendId != mob.Id && _activeMobs.TryGetValue(friendId, out Mob friend))
                        {
                            // Зовем на помощь только тех, кто еще ни с кем не дерется
                            if (friend.CurrentTargetId == null)
                            {
                                var friendTemplate = MobDatabase.GetTemplate(friend.TemplateId);
                                // Если у соседа такой же тег (например, "goblin") -> он агрится на того же врага!
                                if (friendTemplate != null && friendTemplate.GroupTag == template.GroupTag)
                                {
                                    friend.CurrentTargetId = attackerId;
                                }
                            }
                        }
                    }
                }
            }

            // 3. Отправляем всем зрителям пакет о том, что моб получил урон
            var damageEvent = new { @event = "mob_damaged", id = mob.Id, damage = damage, hp = mob.CurrentHp };
            var chunksToNotify = _chunkManager.GetVisibleChunks(mob.Position.X, mob.Position.Y);
            connManager.BroadcastToChunks(chunksToNotify, damageEvent);

            // 4. Проверка на смерть
            if (mob.CurrentHp <= 0)
            {
                // TODO: Реализовать логику смерти (выпадение лута, удаление моба, пакет mob_died)
                RemoveMob(mob.Id);
                var deathEvent = new { @event = "mob_died", id = mob.Id };
                connManager.BroadcastToChunks(chunksToNotify, deathEvent);
            }
        }
    }
}

