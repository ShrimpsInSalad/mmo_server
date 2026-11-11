using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MMOServer.Models;

public class MobTemplate
{
    [JsonPropertyName("TemplateId")]
    public int TemplateId { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("MaxHp")]
    public int MaxHp { get; set; }

    [JsonPropertyName("ChaseDistance")]
    public int ChaseDistance { get; set; }

    [JsonPropertyName("SpeedTicks")]
    public int SpeedTicks { get; set; }

    [JsonPropertyName("AttackType")]
    public string AttackType { get; set; } = "Melee";

    [JsonPropertyName("AttackRange")]
    public int AttackRange { get; set; }

    [JsonPropertyName("Damage")]
    public int Damage { get; set; }

    [JsonPropertyName("CanHeal")]
    public bool CanHeal { get; set; }

    [JsonPropertyName("HealCooldownTicks")]
    public int HealCooldownTicks { get; set; }

    [JsonPropertyName("FleesOnLowHp")]
    public bool FleesOnLowHp { get; set; }

    [JsonPropertyName("GroupTag")]
    public string GroupTag { get; set; } = string.Empty;

    [JsonPropertyName("IsPackMob")]
    public bool IsPackMob { get; set; }

    [JsonPropertyName("Temperament")]
    public string Temperament { get; set; } = "Neutral";

    [JsonPropertyName("VisionRadius")]
    public int VisionRadius { get; set; }
}
