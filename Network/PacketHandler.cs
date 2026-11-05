using MMOServer.Models;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MMOServer.Network;

public class PacketHandler
{
    public static async Task HandleMessage(Player player, string jsonMessage)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(jsonMessage);

            JsonElement root = doc.RootElement;

            if (!root.TryGetProperty("action", out JsonElement actionProperty))
            {
                Console.WriteLine($"[Ошибка] Игрок {player.Id} прислал пакет без поля 'action'");
                return;
            }

            string action = actionProperty.GetString() ?? string.Empty;

            switch (action)
            {
                case "ping":
                    Console.WriteLine($"[Ping] Игрок {player.Name} прислал тестовый пинг.");
                    break;
                case "move":
                    await HandleMove(player, root);
                    break;
                default:
                    Console.WriteLine($"[Внимание] Неизвестная команда '{action}' от игрока {player.Name}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Ошибка JSON] Не удалось распарсить пакет от {player.Name}: {ex.Message}");
        }
    }


    private static async Task HandleMove(Player player, JsonElement root)
    {
        if (root.TryGetProperty("x", out JsonElement xProp) && root.TryGetProperty("y", out JsonElement yProp))
        {
            int targetX = xProp.GetInt32();
            int targetY = yProp.GetInt32();

            int diffX = Math.Abs(targetX - player.Position.X);
            int diffY = Math.Abs(targetY - player.Position.Y);

            bool isValidDistance = diffX <= 1 && diffY <= 1;
            bool isNewTile = diffX > 0 || diffY > 0;
            bool isWall = GameMap.IsWall(targetX, targetY);

            if (isValidDistance && isNewTile && !isWall)
            {
                player.Position = new Position(targetX, targetY);
                Console.WriteLine($"[Движение] Игрок {player.Name} (ID: {player.Id}) перешел в X:{targetX}, Y:{targetY}");

                var responseObj = new
                {
                    @event = "moved", // Собачка '@' нужна, потому что event - зарезервированное слово в C#
                    x = targetX,
                    y = targetY
                };

                string jsonResponse = JsonSerializer.Serialize(responseObj);

                byte[] bytes = Encoding.UTF8.GetBytes(jsonResponse);
                await player.ClientSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
            else
            {
                Console.WriteLine($"[Защита] Отклонен невалидный шаг игрока {player.Name} в X:{targetX}, Y:{targetY}");
            }
        }
    }
}
