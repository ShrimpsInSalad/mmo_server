var builder = WebApplication.CreateBuilder(args);

// Читаем порт, который нам выдаст облако Render (или используем 8080 для тестов)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

// Указываем серверу слушать все подключения на этом порту
builder.WebHost.UseUrls($"http://*:{port}");

var app = builder.Build();

// Если кто-то перейдет по базовой ссылке сервера, возвращаем текст
app.MapGet("/", () => "hello world!");

app.Run();