using CounterStrikeSharp.API;
using DeathrunManagerPlugin;

namespace DeathrunManager
{
    internal class TeamUtilities
    {
        private const int SPECTATOR_TEAM = 1;
        private const int COUNTER_TERRORIST_TEAM = 3;
        private const int TERRORIST_TEAM = 2;

        public static TeamData GetTeamPlayers()
        {
            var allPlayers = Utilities.GetPlayers()
                .Where(p => p.TeamNum != SPECTATOR_TEAM && PlayerValidator.IsValid(p))
                .ToList();

            var counterTerrorists = allPlayers.Where(p => p.TeamNum == COUNTER_TERRORIST_TEAM).ToList();
            var terrorists = allPlayers.Where(p => p.TeamNum == TERRORIST_TEAM).ToList();

            return new TeamData(allPlayers, counterTerrorists, terrorists);
        }
    }
}
