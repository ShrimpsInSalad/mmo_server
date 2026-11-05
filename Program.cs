
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");
var app = builder.Build();

// 1. Включаем поддержку вебсокетов в ядре сервера
app.UseWebSockets();

// 2. Обрабатываем все входящие запросы на корневой адрес "/"
app.Map("/", async context =>
{
    // Проверяем: это игровой клиент стучится по WebSocket?
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        Console.WriteLine("Новый игрок подключился!");

        // Отправляем тестовое приветствие клиенту
        var welcomeMsg = Encoding.UTF8.GetBytes("Привет от C# сервера!");
        await webSocket.SendAsync(new ArraySegment<byte>(welcomeMsg), WebSocketMessageType.Text, true, CancellationToken.None);

        // Создаем буфер (память) для приема сообщений
        var buffer = new byte[1024 * 4];
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        // Бесконечный цикл, который держит соединение открытым
        while (!result.CloseStatus.HasValue)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Игрок пишет: {message}");

            // Эхо-ответ серверу
            var responseMsg = Encoding.UTF8.GetBytes($"C# сервер получил: {message}");
            await webSocket.SendAsync(new ArraySegment<byte>(responseMsg), result.MessageType, result.EndOfMessage, CancellationToken.None);

            // Ждем следующее сообщение
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }
        
        Console.WriteLine("Игрок отключился.");
    }
    else
    {
        // Если это обычный запрос из браузера — отдаем текст
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("Сервер MMO на C# работает!");
    }
});

app.Run();