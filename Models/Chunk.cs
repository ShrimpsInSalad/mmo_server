using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMOServer.Models;

public class Chunk
{
    public int ChunkX { get; set; }
    public int ChunkY { get; set; }

    public HashSet<int> PlayersInside { get; set; } = new HashSet<int>();
    public HashSet<int> MobsInside { get; set; } = new HashSet<int>();

    public Chunk(int cx, int cy)
    {
        ChunkX = cx;
        ChunkY = cy;
    }
}

