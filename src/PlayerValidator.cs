using CounterStrikeSharp.API.Core;

namespace DeathrunManager
{
    internal class PlayerValidator
    {
        public static bool IsValid(CCSPlayerController? player)
        {
            return player?.IsValid == true && player.PlayerPawn?.IsValid == true;
        }
    }
}
