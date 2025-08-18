using CounterStrikeSharp.API.Core;

namespace DeathrunManager
{
    internal class TerroristSelector
    {
        private readonly Random _random = new();
        private readonly PluginSettings _settings;

        public TerroristSelector(PluginSettings settings)
        {
            _settings = settings;
        }

        public CCSPlayerController? SelectRandom(List<CCSPlayerController> candidates)
        {
            if (candidates.Count == 0) return null;

            int selectedIndex = _random.Next(candidates.Count);
            return candidates[selectedIndex];
        }
    }
}
