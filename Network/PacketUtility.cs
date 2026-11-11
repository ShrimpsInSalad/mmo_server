using MMOServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MMOServer.Network;

public static class PacketUtility
{
    public static async Task SendAsync(WebSocket socket, object payload)
    {
        if (socket.State != WebSocketState.Open) return;

        string json = JsonSerializer.Serialize(payload);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        await socket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }

    public static void EnqueuePacket(Player player, object payload)
    {
        lock (player.BufferLock)
        {
            player.OutgoingPackets.Add(payload);
        }
    }

    public static async Task SendBufferedPacketsAsync(Player player)
    {
        List<object> packetsToSend;

        lock (player.BufferLock)
        {
            if (player.OutgoingPackets.Count == 0) return;

            packetsToSend = new List<object>(player.OutgoingPackets);
            player.OutgoingPackets.Clear();
        }

        if (player.ClientSocket.State != WebSocketState.Open) return;

        string json = JsonSerializer.Serialize(packetsToSend);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        try
        {
            await player.ClientSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception ex)    
        {
            Console.WriteLine($"[Сеть] Ошибка отправки пакетов игроку {player.Name}: {ex.Message}");
        }
    }
}

