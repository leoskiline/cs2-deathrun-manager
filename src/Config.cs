using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("dr_prefix")]
    public string DrPrefix { get; set; } = "DR Manager";

    [JsonPropertyName("dr_enabled")]
    public int DrEnabled { get; set; } = 1;

    [JsonPropertyName("dr_allow_ct_go_spec")]
    public int DrAllowCTGoSpec { get; set; } = 1;

    [JsonPropertyName("dr_only_deathrun_maps")]
    public int DrOnlyDeathrunMaps { get; set; } = 1;

    [JsonPropertyName("dr_enable_bunnyhop")]
    public int DrEnableBunnyhop { get; set; } = 1;

    [JsonPropertyName("dr_terrorist_no_fall_damage")]
    public int DrTerroristNoFallDamage { get; set; } = 1;

    [JsonPropertyName("dr_enable_detailed_logging")]
    public int DrEnableDetailedLogging { get; set; } = 1;

    [JsonPropertyName("dr_log_retention_days")]
    public int DrLogRetentionDays { get; set; } = 7;

    [JsonPropertyName("dr_velocity_multiplier_tr")]
    public float DrVelocityMultiplierTR { get; set; } = 1.75f;
}
