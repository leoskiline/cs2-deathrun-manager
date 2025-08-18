using CounterStrikeSharp.API.Core;

namespace DeathrunManager
{
    internal class GameState
    {
        public CCSPlayerController? SelectedTerrorist { get; set; }
        public bool IsDeathrunMap { get; set; }
    }
}
