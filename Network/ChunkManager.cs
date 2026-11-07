using MMOServer.Config;
using MMOServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMOServer.Network;

public class ChunkManager
{
    public int ViewRadius { get; set; } = 1;

    private Chunk[] _chunks;

    public ChunkManager()
    {
        _chunks = new Chunk[MapConfig.ChunksX * MapConfig.ChunksY];

        for (int y = 0; y < MapConfig.ChunksY; y++)
        {
            for (int x = 0; x < MapConfig.ChunksX; x++)
            {
                _chunks[y * MapConfig.ChunksX + x] = new Chunk(x, y);
            }
        }
    }

    public (int cx, int cy) GetChunkCoords(int tileX, int tileY)
    {
        int cx = tileX >> MapConfig.ChunkShift;
        int cy = tileY >> MapConfig.ChunkShift;

        return (cx, cy);
    }

    public Chunk GetChunk(int cx, int cy)
    {
        if (cx < 0 || cx >= MapConfig.ChunksX || cy < 0 || cy >= MapConfig.ChunksY)
            return null;

        return _chunks[cy * MapConfig.ChunksX + cx];
    }

    public List<Chunk> GetVisibleChunks(int tileX, int tileY)
    {
        var visible = new List<Chunk>();

        var (centerCx, centerCy) = GetChunkCoords(tileX, tileY);

        for (int y = centerCy - ViewRadius; y <= centerCy + ViewRadius; y++)
        {
            for (int x = centerCx - ViewRadius; x <= centerCx + ViewRadius; x++)
            {
                if (x >= 0 && x < MapConfig.ChunksX && y >= 0 && y < MapConfig.ChunksY)
                {
                    visible.Add(_chunks[y * MapConfig.ChunksX + x]);
                }
            }
        }
        return visible;
    }
}

