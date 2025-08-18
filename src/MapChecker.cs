namespace DeathrunManager
{
    internal class MapChecker
    {
        private static readonly string[] MapPrefixes = { "dr_", "deathrun_" };

        public bool IsDeathrunMap(string mapName)
        {
            var normalizedMapName = mapName.ToLowerInvariant();
            return MapPrefixes.Any(prefix => normalizedMapName.StartsWith(prefix));
        }
    }
}
