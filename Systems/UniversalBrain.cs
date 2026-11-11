using MMOServer.Models;
using MMOServer.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMOServer.Systems;

public class UniversalBrain : IMobBrain
{
    private static readonly Random _rng = new Random();

    public void Update(Mob body, ulong currentServerTick, ConnectionManager connManager, MobManager mobManager)
    {
        var template = MobDatabase.GetTemplate(body.TemplateId);

        if (template == null) return;

        bool isLowHp = body.CurrentHp < (template.MaxHp * 0.2f);

        if (isLowHp && template.CanHeal && (currentServerTick - body.LastHealTick >= (ulong)template.HealCooldownTicks))
        {
            int healAmount = (int)(template.MaxHp * 0.2f);
            body.CurrentHp = Math.Min(template.MaxHp, body.CurrentHp + healAmount);
            body.LastHealTick = currentServerTick;

            var healEvent = new { @event = "mob_healed", id = body.Id, hp = body.CurrentHp };
            var visibleChunks = connManager.Chunks.GetVisibleChunks(body.Position.X, body.Position.Y);
            connManager.BroadcastToChunks(visibleChunks, healEvent);
        }

        bool shouldPanic = (isLowHp && template.FleesOnLowHp) || (template.Temperament == "Coward" && body.CurrentTargetId != null);

        body.IsPanicking = shouldPanic;

        if (body.CurrentTargetId == null)
        {

            PerformRandomWander(body, mobManager, connManager);

            switch (template.Temperament)
            {
                case "Aggressive":
                    // TODO: Написать логику случайного брожения (Random Wander) вокруг SpawnPosition.
                    // TODO: рассылать уведомление о шаге игрока мобам в видимых чанках или запросить у ChunkManager список всех игроков в радиусе VisionRadius.
                    // Если игрок найден -> записать его ID в body.CurrentTargetId
                    break;
                case "Neutral":
                    // TODO: Написать логику случайного брожения (Random Wander) вокруг SpawnPosition.
                    // Если моба атакуют- записать ID атакующено в body.CurrentTargetId
                    break;
                case "Coward":
                    // TODO: Написать логику случайного брожения (Random Wander) вокруг SpawnPosition.
                    // Если моба атакуют- записать ID атакующено в body.CurrentTargetId, а IsPanicking становится true
                    // Использовать тот же метод что и для побега при лоу хп - моб ищет клетку куда шагнуть в противоположную сторону от body.CurrentTargetId
                    break;
            }
        }
        else
        {
            var targetPlayer = connManager.GetPlayer(body.CurrentTargetId.Value);

            if (targetPlayer == null)
            {
                body.CurrentTargetId = null;
                return;
            }

            int dist = Math.Max(Math.Abs(body.Position.X - targetPlayer.Position.X), Math.Abs(body.Position.Y - targetPlayer.Position.Y));

            if (body.IsPanicking)
            {
                PerformFlee(body, targetPlayer.Position, mobManager, connManager);
                if (dist > 15) body.CurrentTargetId = null;
                return;
            }

            if (dist > template.ChaseDistance)
            {
                body.CurrentTargetId = null;
                return;
            }

            if (template.AttackType == "Melee")
            {
                if (dist > 1) MoveTowards(body, targetPlayer.Position, mobManager, connManager);
                else PerformAttack(body, targetPlayer, template.Damage, connManager);
            }
            else if (template.AttackType == "Ranged" || template.AttackType == "Magic")
            {
                if (dist > template.AttackRange) MoveTowards(body, targetPlayer.Position, mobManager, connManager);
                else if (dist < 3) PerformFlee(body, targetPlayer.Position, mobManager, connManager); // Кайтинг использует тот же метод побега
                else PerformAttack(body, targetPlayer, template.Damage, connManager);
            }
        }
    }

    private void PerformRandomWander(Mob body, MobManager mobManager, ConnectionManager connManager)
    {
        if (_rng.Next(0, 100) >= 10) return;

        int dx = _rng.Next(-1, 2);
        int dy = _rng.Next(-1, 2);

        if (dx == 0 && dy == 0) return;

        int targetX = body.Position.X + dx;
        int targetY = body.Position.Y + dy;

        int distFromSpawn = Math.Max(Math.Abs(targetX - body.SpawnPosition.X), Math.Abs(targetY - body.SpawnPosition.Y));
        if (distFromSpawn <= 10 && GameMap.CanMove(body.Position, targetX, targetY))
        {
            mobManager.UpdateMobPosition(body, new Position(targetX, targetY), connManager);
        }
    }

    private void PerformFlee(Mob body, Position dangerPos, MobManager mobManager, ConnectionManager connManager)
    {
        int dx = Math.Sign(body.Position.X - dangerPos.X);
        int dy = Math.Sign(body.Position.Y - dangerPos.Y);

        int targetX = body.Position.X + dx;
        int targetY = body.Position.Y + dy;

        if (GameMap.CanMove(body.Position, targetX, targetY))
        {
            mobManager.UpdateMobPosition(body, new Position(targetX, targetY), connManager);
        }
    }

    private void MoveTowards(Mob body, Position targetPos, MobManager mobManager, ConnectionManager connManager)
    {
        int dx = Math.Sign(targetPos.X - body.Position.X);
        int dy = Math.Sign(targetPos.Y - body.Position.Y);

        int targetX = body.Position.X + dx;
        int targetY = body.Position.Y + dy;

        if (GameMap.CanMove(body.Position, targetX, targetY))
        {
            mobManager.UpdateMobPosition(body, new Position(targetX, targetY), connManager);
        }
        else if (dx != 0 && GameMap.CanMove(body.Position, body.Position.X + dx, body.Position.Y))
        {
            mobManager.UpdateMobPosition(body, new Position(body.Position.X + dx, body.Position.Y), connManager);
        }
        else if (dy != 0 && GameMap.CanMove(body.Position, body.Position.X, body.Position.Y + dy))
        {
            mobManager.UpdateMobPosition(body, new Position(body.Position.X, body.Position.Y + dy), connManager);
        }
    }

    private void PerformAttack(Mob body, Player target, int damage, ConnectionManager connManager)
    {
        var attackEvent = new { @event = "mob_attack", id = body.Id, target_id = target.Id, damage = damage };
        var visibleChunks = connManager.Chunks.GetVisibleChunks(body.Position.X, body.Position.Y);
        connManager.BroadcastToChunks(visibleChunks, attackEvent);

        // TODO: Вызвать метод получения урона у игрока (например, target.TakeDamage(damage))
    }
}
