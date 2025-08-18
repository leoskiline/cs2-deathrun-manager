using CounterStrikeSharp.API.Modules.Utils;

namespace DeathrunManager
{
    internal class PluginSettings
    {
        public bool Enabled { get; set; } = true;
        public bool AllowCTGoSpec { get; set; } = true;
        public bool OnlyDeathrunMaps { get; set; } = true;
        public bool EnableBunnyhop { get; set; } = true;
        public bool TerroristNoFallDamage { get; set; } = true;
        public bool EnableDetailedLogging { get; set; } = false;
        public int LogRetentionDays { get; set; } = 7;
        public float VelocityMultiplierTR { get; set; } = 1.75f;

        private string _prefix = "DR Manager";
        public string FormattedPrefix => $"{ChatColors.Gold}[{ChatColors.Red}{_prefix}{ChatColors.Gold}]{ChatColors.Default}";

        public void SetPrefix(string prefix) => _prefix = prefix;

        public void ApplyConfig(PluginConfig config)
        {
            _prefix = config.DrPrefix;
            Enabled = config.DrEnabled == 1;
            AllowCTGoSpec = config.DrAllowCTGoSpec == 1;
            OnlyDeathrunMaps = config.DrOnlyDeathrunMaps == 1;
            EnableBunnyhop = config.DrEnableBunnyhop == 1;
            TerroristNoFallDamage = config.DrTerroristNoFallDamage == 1;
            EnableDetailedLogging = config.DrEnableDetailedLogging == 1;
            LogRetentionDays = config.DrLogRetentionDays;
            VelocityMultiplierTR = config.DrVelocityMultiplierTR;
        }
    }
}
