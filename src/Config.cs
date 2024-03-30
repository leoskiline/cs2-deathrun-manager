using System;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("DeathrunEnabled")] public int DrEnabled { get; set; } = 1;
    [JsonPropertyName("DeathrunPrefix")] public string DrPrefix { get; set; } = "[DR Manager]";
    [JsonPropertyName("DeathrunAllowCTGoSpec")] public int DrAllowCTGoSpec { get; set; } = 1;
    [JsonPropertyName("DeathrunVelocityMultiplierTR")] public float DrVelocityMultiplierTR { get; set; } = (float)1.75;
}
