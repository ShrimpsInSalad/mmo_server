using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMOServer.Config;

public static class MapConfig
{
    // Единственный вход для параметра размера карты
    public const int MapWidth = 512;
    public const int MapHeight = 512;

    public const int ChunkShift = 4;

    public const int ChunkSize = 1 << ChunkShift;

    // Количество чанков по осям
    public const int ChunksX = MapWidth >> ChunkShift;
    public const int ChunksY = MapHeight >> ChunkShift;

    static MapConfig()
    {
        ValidateConfig();
    }

    private static void ValidateConfig()
    {
        if (!IsPowerOfTwo(MapWidth))
            throw new Exception($"[КРИТИЧЕСКАЯ ОШИБКА] Ширина карты ({MapWidth}) должна быть степенью двойки!");

        if (!IsPowerOfTwo(MapHeight))
            throw new Exception($"[КРИТИЧЕСКАЯ ОШИБКА] Высота карты ({MapHeight}) должна быть степенью двойки!");

        if (MapWidth % ChunkSize != 0 || MapHeight % ChunkSize != 0)
            throw new Exception($"[КРИТИЧЕСКАЯ ОШИБКА] Размеры карты должны нацело делиться на размер чанка ({ChunkSize})!");

        Console.WriteLine($"[Конфиг] Карта: {MapWidth}x{MapHeight}. Чанки: {ChunksX}x{ChunksY} (размер чанка: {ChunkSize}).");
    }

    private static bool IsPowerOfTwo(int n)
    {
        return (n > 0) && ((n & (n - 1)) == 0);
    }

    public static void Init() { }
}