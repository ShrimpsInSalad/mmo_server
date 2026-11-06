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
    public const int Width = 20;
    public const int Height = 20;

    private static byte[] _grid = new byte[Width * Height];

    static GameMap()
    {
        AddWall(12, 10, WallDirection.WallEast);
        AddWall(12, 11, WallDirection.WallEast);
        AddWall(12, 12, WallDirection.WallEast);
        AddWall(12, 12, WallDirection.WallSouth);
    }

    public static void AddWall(int x, int y, WallDirection dir)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;

        _grid[y * Width + x] |= (byte)dir;

        if (dir == WallDirection.WallNorth && y > 0) _grid[(y - 1) * Width + x] |= (byte)WallDirection.WallSouth;
        else if (dir == WallDirection.WallSouth && y < Height - 1) _grid[(y + 1) * Width + x] |= (byte)WallDirection.WallNorth;
        else if (dir == WallDirection.WallEast && x < Width - 1) _grid[y * Width + (x + 1)] |= (byte)WallDirection.WallWest;
        else if (dir == WallDirection.WallWest && x > 0) _grid[y * Width + (x - 1)] |= (byte)WallDirection.WallEast;


    }

    public static bool CanMove(Position from, int toX, int toY)
    {
        if (toX < 0 || toX >= Width || toY < 0 || toY >= Height) return false;

        byte currentTile = _grid[from.Y * Width + from.X];

        if (toY < from.Y && toX == from.X) return (currentTile & (byte)WallDirection.WallNorth) == 0;

        // Движение на Юг (Y увеличился)
        if (toY > from.Y && toX == from.X) return (currentTile & (byte)WallDirection.WallSouth) == 0;

        // Движение на Восток (X увеличился)
        if (toX > from.X && toY == from.Y) return (currentTile & (byte)WallDirection.WallEast) == 0;

        // Движение на Запад (X уменьшился)
        if (toX < from.X && toY == from.Y) return (currentTile & (byte)WallDirection.WallWest) == 0;

        if (toX > from.X && toY < from.Y) // Северо-Восток
            return (currentTile & (byte)WallDirection.WallNorth) == 0 && (currentTile & (byte)WallDirection.WallEast) == 0;

        if (toX > from.X && toY > from.Y) // Юго-Восток
            return (currentTile & (byte)WallDirection.WallSouth) == 0 && (currentTile & (byte)WallDirection.WallEast) == 0;

        if (toX < from.X && toY < from.Y) // Северо-Запад
            return (currentTile & (byte)WallDirection.WallNorth) == 0 && (currentTile & (byte)WallDirection.WallWest) == 0;

        if (toX < from.X && toY > from.Y) // Юго-Запад
            return (currentTile & (byte)WallDirection.WallSouth) == 0 && (currentTile & (byte)WallDirection.WallWest) == 0;

        return true; // Если ни одно условие блокировки не сработало

    }
}
