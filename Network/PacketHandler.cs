using MMOServer.Models;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MMOServer.Network;

public class PacketHandler
{
    private static ConcurrentQueue<(Player player, string action)> _actionQueue = new ConcurrentQueue<(Player, string)>();

    public static void EnqueueMessage(Player player, string jsonMessage)
    {
        _actionQueue.Enqueue((player, jsonMessage));
    }

    public static async Task ProcessQueue(ConnectionManager manager, MobManager mobManager)
    {
        int processed = 0;

        while (processed < 100 && _actionQueue.TryDequeue(out var item))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(item.action);
                JsonElement root = doc.RootElement;
                if (!root.TryGetProperty("action", out JsonElement actionProp)) continue;

                string actionType = actionProp.GetString();
                if (actionType == "move")
                {
                    await HandleMove(item.player, root, manager, mobManager);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки: {ex.Message}");
            }

            processed++;

        }
    }

    private static async Task HandleMove(Player player, JsonElement root, ConnectionManager manager, MobManager mobManager)
    {
        if (root.TryGetProperty("x", out JsonElement xProp) && root.TryGetProperty("y", out JsonElement yProp))
        {
            int targetX = xProp.GetInt32();
            int targetY = yProp.GetInt32();

            int diffX = Math.Abs(targetX - player.Position.X);
            int diffY = Math.Abs(targetY - player.Position.Y);

            bool isValidDistance = diffX <= 1 && diffY <= 1;
            bool isNewTile = diffX > 0 || diffY > 0;
            bool canMove = GameMap.CanMove(player.Position, targetX, targetY);

            if (isValidDistance && isNewTile && canMove)
            {
                manager.UpdatePlayerPosition(player, new Position(targetX, targetY), mobManager);
                Console.WriteLine($"[Движение] {player.Name} (ID: {player.Id}) перешел в X:{targetX}, Y:{targetY}");
            }
            else
            {
                Console.WriteLine($"[Защита] Отклонен невалидный шаг игрока {player.Name} в X:{targetX}, Y:{targetY}");
            }
        }
        else
        {
            Console.WriteLine($"[Ошибка Движения] В пакете от {player.Name} нет координат x или y!");
        }
    }
}
