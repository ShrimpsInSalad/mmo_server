using System;
using System.Collections;

namespace MMOServer.Models;

public static class GameMap
{
    public const int Width = 20;
    public const int Height = 20;

    private static BitArray _walls = new BitArray(Width * Height);

    static GameMap()
    {
        SetWall(12, 10);
        SetWall(12, 11);
        SetWall(11, 11);
    }

    private static void SetWall(int x, int y)
    {
        _walls[y * Width + x] = true;
    }

    public static bool IsWall(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            return true; // Граница мира работает как невидимая стена
        }

        // Возвращаем true, если в BitArray стоит единичка (стена), и false если пол
        return _walls[y * Width + x];
    }
}
