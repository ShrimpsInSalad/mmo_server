using MMOServer.Config;
using System;
using System.Collections;

namespace MMOServer.Models;

[Flags]
public enum WallDirection : byte
{
    // 0000 0000 — Пустая клетка (обычный пол)
    None = 0,

    // --- ПЕРВЫЕ 4 БИТА: СТЕНЫ НА ГРАНИЦАХ ---
    WallNorth = 1 << 0, // Значение 1   (Двоичное: 0000 0001)
    WallEast = 1 << 1, // Значение 2   (Двоичное: 0000 0010)
    WallSouth = 1 << 2, // Значение 4   (Двоичное: 0000 0100)
    WallWest = 1 << 3, // Значение 8   (Двоичное: 0000 1000)

    // --- ОСТАВШИЕСЯ 4 БИТА: ПРАВИЛА ИГРЫ ---
    IsBlocksVision = 1 << 4, // Значение 16  (Двоичное: 0001 0000)
    SafeZone = 1 << 5, // Значение 32  (Двоичное: 0010 0000) теоретический параметр, в будущем под замену под реальные нужды
    MultiCombat = 1 << 6, // Значение 64  (Двоичное: 0100 0000) теоретический параметр, в будущем под замену под реальные нужды
    IsNotWalkable = 1 << 7  // Значение 128 (Двоичное: 1000 0000)
}

public static class GameMap
{   
    private static byte[] _grid;

    static GameMap()
    {
        LoadMap();
    }

    private static void LoadMap()
    {
        string filePath = "world_map.bin";

        if (File.Exists(filePath))
        {
            _grid = File.ReadAllBytes(filePath);

            int expectedSize = MapConfig.MapWidth * MapConfig.MapHeight;

            if (_grid.Length != expectedSize)
            {
                throw new Exception($"[Ошибка Карты] Размер файла {_grid.Length} байт не совпадает с конфигом ({expectedSize} байт)!");
            }

            Console.WriteLine($"[Карта] Успешно загружена! Размер: {_grid.Length} байт.");
        }
        else
        {
            Console.WriteLine($"[Карта] ВНИМАНИЕ! Файл {filePath} не найден. Создана пустая карта.");
            _grid = new byte[MapConfig.MapWidth * MapConfig.MapHeight];
        }
    }

    private static bool HasWall(int x, int y, WallDirection dir)
    {
        if (x < 0 || x >= MapConfig.MapWidth || y < 0 || y >= MapConfig.MapHeight) return true;
        byte tile = _grid[y * MapConfig.MapWidth + x];
        if ((tile & (byte)WallDirection.IsNotWalkable) != 0) return true;
        return (tile & (byte)dir) != 0;
    }

    public static bool CanMove(Position from, int toX, int toY)
    {
        if (toX < 0 || toX >= MapConfig.MapWidth || toY < 0 || toY >= MapConfig.MapHeight) return false;

        byte currentTile = _grid[from.Y * MapConfig.MapWidth + from.X];

        byte targetTile = _grid[toY * MapConfig.MapWidth + toX];
        if ((targetTile & (byte)WallDirection.IsNotWalkable) != 0) return false;

        if (toY < from.Y && toX == from.X) return (currentTile & (byte)WallDirection.WallNorth) == 0;
        if (toY > from.Y && toX == from.X) return (currentTile & (byte)WallDirection.WallSouth) == 0;
        if (toX > from.X && toY == from.Y) return (currentTile & (byte)WallDirection.WallEast) == 0;
        if (toX < from.X && toY == from.Y) return (currentTile & (byte)WallDirection.WallWest) == 0;

        if (toX > from.X && toY < from.Y) // Северо-Восток
            return !HasWall(from.X, from.Y, WallDirection.WallNorth) &&
                   !HasWall(from.X, from.Y, WallDirection.WallEast) &&
                   !HasWall(toX, from.Y, WallDirection.WallNorth) &&
                   !HasWall(from.X, toY, WallDirection.WallEast);

        if (toX > from.X && toY > from.Y) // Юго-Восток
            return !HasWall(from.X, from.Y, WallDirection.WallSouth) &&
                   !HasWall(from.X, from.Y, WallDirection.WallEast) &&
                   !HasWall(toX, from.Y, WallDirection.WallSouth) &&
                   !HasWall(from.X, toY, WallDirection.WallEast);

        if (toX < from.X && toY < from.Y) // Северо-Запад
            return !HasWall(from.X, from.Y, WallDirection.WallNorth) &&
                   !HasWall(from.X, from.Y, WallDirection.WallWest) &&
                   !HasWall(toX, from.Y, WallDirection.WallNorth) &&
                   !HasWall(from.X, toY, WallDirection.WallWest);

        if (toX < from.X && toY > from.Y) // Юго-Запад
            return !HasWall(from.X, from.Y, WallDirection.WallSouth) &&
                   !HasWall(from.X, from.Y, WallDirection.WallWest) &&
                   !HasWall(toX, from.Y, WallDirection.WallSouth) &&
                   !HasWall(from.X, toY, WallDirection.WallWest);

        return true; // Если ни одно условие блокировки не сработало

    }
    public static void Init() { }
}
