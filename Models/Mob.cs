using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMOServer.Models;

public interface IMobBrain
{
    void Update(Mob body, ulong currentServerTick);
}

public class Mob
{
    public int Id { get; set; }
    public int TemplateId { get; set; }

    public Position Position { get; set; }
    public Position SpawnPosition { get; set; }

    public int ChunkX { get; set; } = -1;
    public int ChunkY { get; set; } = -1;

    public int CurrentHp { get; set; }

    public int? CurrentTargetId { get; set; }

    public int MobSlot { get; private set; }

    public IMobBrain Brain { get; set; }

    public Mob(int id, int templateId, Position spawnPos)
    {
        Id = id;
        TemplateId = templateId;
        Position = spawnPos;
        SpawnPosition = spawnPos;

        MobSlot = id % 20;

        var template = MobDatabase.GetTemplate(templateId);

        if (template != null)
        {
            CurrentHp = template.MaxHp;
        }
    }
}