using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MMOServer.Config;
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

MobDatabase.Init();
MapConfig.Init();
GameMap.Init();

app.UseWebSockets();

ConnectionManager connectionManager = new ConnectionManager();

MobManager mobManager = new MobManager(connectionManager.Chunks);



app.Map("/ws", async context =>
{

    if (context.WebSockets.IsWebSocketRequest)
    {
        using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

        int randomId = new Random().Next(1, 1000);
        Position startPos = new Position(10, 10);

        Player newPlayer = new Player(randomId, $"Player_{randomId}", webSocket, startPos);

        connectionManager.AddPlayer(newPlayer);

        var visibleChunks = connectionManager.Chunks.GetVisibleChunks(startPos.X, startPos.Y);
        var visiblePlayersIds = visibleChunks.SelectMany(c => c.PlayersInside).ToHashSet();

        var existingPlayers = connectionManager.GetAllPlayers()
                    .Where(p => p.Id != newPlayer.Id && visiblePlayersIds.Contains(p.Id) && p.ClientSocket.State == WebSocketState.Open)
                    .Select(p => new {
                        id = p.Id,
                        x = p.Position.X,
                        y = p.Position.Y
                    }).ToList();

        var welcomeObj = new
        {
            @event = "welcome",
            my_id = newPlayer.Id,
            x = newPlayer.Position.X,
            y = newPlayer.Position.Y,
            others = existingPlayers
        };

        await PacketUtility.SendAsync(newPlayer.ClientSocket, welcomeObj);

        var joinedObj = new { @event = "player_joined", id = newPlayer.Id, x = newPlayer.Position.X, y = newPlayer.Position.Y };
        await connectionManager.BroadcastToVisibleAsync(newPlayer.Position.X, newPlayer.Position.Y, joinedObj, newPlayer.Id);

        await ReciveMessages(newPlayer);

        connectionManager.RemovePlayer(newPlayer.Id);

        var leftObj = new { @event = "player_left", id = newPlayer.Id };
        await connectionManager.BroadcastToVisibleAsync(newPlayer.Position.X, newPlayer.Position.Y, leftObj, newPlayer.Id);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapGet("/", () => "Server is alive");

_ = Task.Run(async () =>
{
    using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));

    ulong serverTick = 0;

    while (await timer.WaitForNextTickAsync())
    {
        serverTick++;

        await PacketHandler.ProcessQueue(connectionManager);

        mobManager.UpdateActiveMobs(serverTick, connectionManager.GetAllPlayers());    
    }
});

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

                PacketHandler.EnqueueMessage(player, jsonMessage);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Ошибка связи] Игрок {player.Name} потерял соединение: {ex.Message}");
    }
}
