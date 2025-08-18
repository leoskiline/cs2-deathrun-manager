using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace DeathrunManager
{
    internal class TeamManager
    {
        private const int SPECTATOR_TEAM = 1;
        private const int TERRORIST_TEAM = 2;
        private const int COUNTER_TERRORIST_TEAM = 3;

        private readonly PluginSettings _settings;

        public TeamManager(PluginSettings settings)
        {
            _settings = settings;
        }

        public HookResult HandleTeamChange(CCSPlayerController player, CommandInfo info)
        {
            int currentTeam = player.PlayerPawn.Value!.TeamNum;

            if (currentTeam == SPECTATOR_TEAM)
            {
                player.ChangeTeam(CsTeam.CounterTerrorist);
                return HookResult.Handled;
            }

            if (!int.TryParse(info.GetArg(1), out int targetTeam))
                return HookResult.Continue;

            if (_settings.AllowCTGoSpec && currentTeam == COUNTER_TERRORIST_TEAM && targetTeam == SPECTATOR_TEAM)
                return HookResult.Continue;

            var teamData = TeamUtilities.GetTeamPlayers();

            if (teamData.CounterTerrorists.Count > 0 && currentTeam == TERRORIST_TEAM)
            {
                player.PrintToChat($"{_settings.FormattedPrefix} {ChatColors.LightRed}Não é possível mudar de time{ChatColors.Default}");
                return HookResult.Handled;
            }

            if (targetTeam is TERRORIST_TEAM or SPECTATOR_TEAM)
            {
                player.PrintToChat($"{_settings.FormattedPrefix} {ChatColors.Orange}Time limitado{ChatColors.Default}");
                return HookResult.Handled;
            }

            return HookResult.Continue;
        }
    }
}
