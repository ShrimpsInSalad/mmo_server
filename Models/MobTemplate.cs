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

    [JsonPropertyName("DefaultBrain")]
    public string DefaultBrain { get; set; } = string.Empty;
}
