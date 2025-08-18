using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace DeathrunManager
{
    internal class WeaponManager
    {
        public void RemoveWeaponsFromAliveCTs()
        {
            try
            {
                var teamData = TeamUtilities.GetTeamPlayers();
                var aliveCTs = GetAlivePlayers(teamData.CounterTerrorists);

                foreach (var player in aliveCTs)
                {
                    RemovePlayerWeapons(player);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DR Manager] Erro geral ao remover armas dos CTs vivos: {ex.Message}");
            }
        }

        private static List<CCSPlayerController> GetAlivePlayers(List<CCSPlayerController> players)
        {
            return players.Where(player =>
            {
                if (!PlayerValidator.IsValid(player)) return false;

                var playerPawn = player.PlayerPawn?.Value;
                return playerPawn?.IsValid == true && playerPawn.Health > 0;
            }).ToList();
        }

        private static void RemovePlayerWeapons(CCSPlayerController player)
        {
            try
            {
                player.RemoveWeapons();
                player.GiveNamedItem(CsItem.Knife);
                Console.WriteLine($"[DR Manager] Armas removidas do jogador CT vivo: {player.PlayerName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DR Manager] Erro ao remover armas do jogador {player.PlayerName}: {ex.Message}");
            }
        }

        public void RemoveWeaponsOnTheGround()
        {
            try
            {
                var weaponEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("weapon_");
                var weaponsToRemove = GetUnownedWeapons(weaponEntities);

                foreach (var weapon in weaponsToRemove)
                {
                    weapon.Remove();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DR Manager] Erro ao remover armas do chão: {ex.Message}");
                ApplyWeaponCleanupFallback();
            }
        }

        private static List<CBaseEntity> GetUnownedWeapons(IEnumerable<CBaseEntity> weaponEntities)
        {
            return weaponEntities
                .Where(weapon => weapon.IsValid &&
                               weapon.DesignerName.StartsWith("weapon_") &&
                               !HasOwner(weapon))
                .ToList();
        }

        private static bool HasOwner(CBaseEntity weapon)
        {
            try
            {
                if (weapon is CCSWeaponBase weaponBase)
                {
                    var owner = weaponBase.OwnerEntity?.Value;
                    return owner?.IsValid == true;
                }
            }
            catch
            {
                // Se der erro ao acessar, assumir que não tem dono
            }
            return false;
        }

        private static void ApplyWeaponCleanupFallback()
        {
            try
            {
                Server.ExecuteCommand("mp_weapons_allow_map_placed 0");
                Server.ExecuteCommand("mp_weapons_allow_map_placed 1");
            }
            catch (Exception cmdEx)
            {
                Console.WriteLine($"[DR Manager] Erro no fallback de limpeza: {cmdEx.Message}");
            }
        }
    }
}
