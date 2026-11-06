using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MMOServer.Models;
using MMOServer.Network;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

ConnectionManager connectionManager = new ConnectionManager();

app.Map("/ws", async context =>
{

    if (context.WebSockets.IsWebSocketRequest)
    {
        using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

        int randomId = new Random().Next(1, 1000);
        Position startPos = new Position(10, 10);

        Player newPlayer = new Player(randomId, $"Player_{randomId}", webSocket, startPos);

        connectionManager.AddPlayer(newPlayer);

        var welcomeObj = new
        {
            @event = "welcome",
            my_id = newPlayer.Id,
            x = newPlayer.Position.X,
            y = newPlayer.Position.Y
        };
        string welcomeJson = JsonSerializer.Serialize(welcomeObj);
        await newPlayer.ClientSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(welcomeJson)), WebSocketMessageType.Text, true, CancellationToken.None);

        var joinedObj = new { @event = "player_joined", id = newPlayer.Id, x = newPlayer.Position.X, y = newPlayer.Position.Y };
        await connectionManager.BroadcastAsync(JsonSerializer.Serialize(joinedObj));


        await ReciveMessages(newPlayer);

        connectionManager.RemovePlayer(newPlayer.Id);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapGet("/", () => "Server is alive");

app.Run();

async Task ReciveMessages(Player player)
{
    var buffer = new byte[1024];

    try
    {
        while (player.ClientSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await player.ClientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                string jsonMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                await PacketHandler.HandleMessage(player, jsonMessage, connectionManager);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Ошибка связи] Игрок {player.Name} потерял соединение: {ex.Message}");
    }
}
