# 1. Берем образ с инструментами разработчика для сборки (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# 2. Копируем наши исходники внутрь контейнера
COPY . .

# 3. Компилируем проект (превращаем C# код в исполняемый файл)
RUN dotnet publish -c Release -o /app

# 4. Берем супер-легкий образ только для ЗАПУСКА (без инструментов разработчика)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# 5. Команда для запуска нашего сервера
ENTRYPOINT ["dotnet", "MMOServer.dll"]