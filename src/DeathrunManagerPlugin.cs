using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace DeathrunManagerPlugin;

public class DeathrunManagerPlugin : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "Deathrun Manager Plugin";
    public override string ModuleVersion => "0.2.0";
    public override string ModuleAuthor => "Psycho";
    public override string ModuleDescription => "Deathrun Manager plugin for CounterStrike Sharp";

    // Constantes para teams
    private const int SPECTATOR_TEAM = 1;
    private const int TERRORIST_TEAM = 2;
    private const int COUNTER_TERRORIST_TEAM = 3;

    // Configurações do plugin
    private bool _enabled = true;
    private bool _allowCTGoSpec = true;
    private bool _onlyDeathrunMaps = true;
    private bool _enableBunnyhop = true;
    private float _velocityMultiplierTR = 1.75f;
    private string _prefix = $"{ChatColors.White}[{ChatColors.Red}DR Manager{ChatColors.White}]{ChatColors.Default}";
    private string[] _mapPrefixes = { "dr_", "deathrun_" };

    // Comandos bloqueados
    private readonly string[] _blockedCommands = { "kill", "killvector", "explodevector", "explode" };

    // Estado do plugin
    private CCSPlayerController? _selectedTerrorist;
    private bool _convarLoaded = false;
    private bool _isDeathrunMap = false;

    public required PluginConfig Config { get; set; }

    public override void Load(bool hotReload)
    {
        // Verificar se o mapa atual é um mapa de deathrun
        CheckIfDeathrunMap();

        // Registrar listener para atualização do servidor
        RegisterListener<Listeners.OnServerPreWorldUpdate>(_ => OnServerPreWorldUpdate());

        // Registrar listener para mudança de mapa
        RegisterListener<Listeners.OnMapStart>(mapName => OnMapStart(mapName));

        // Registrar listeners para comandos bloqueados apenas em mapas de deathrun
        if (ShouldPluginBeActive())
        {
            foreach (string command in _blockedCommands)
            {
                AddCommandListener(command, CommandListener_BlockCommands);
            }

            // Registrar listener para mudança de time
            AddCommandListener("jointeam", CommandListener_JoinTeam);
        }
    }

    private void OnServerPreWorldUpdate()
    {
        if (_convarLoaded) return;

        Console.WriteLine($"[DR Manager] Carregando configurações do servidor...");

        // Configurar convars do servidor
        Server.ExecuteCommand("mp_t_default_secondary 0");
        Server.ExecuteCommand("mp_ct_default_secondary 0");
        Server.ExecuteCommand("mp_autoteambalance 0");
        Server.ExecuteCommand("mp_limitteams 0");

        // Configurar bunnyhop se habilitado
        ApplyBunnyhopSettings();

        _convarLoaded = true;
    }

    public void OnConfigParsed(PluginConfig config)
    {
        ValidateConfig(config);
        ApplyConfig(config);
        Config = config;
    }

    private void ValidateConfig(PluginConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.DrPrefix))
        {
            errors.Add("DrPrefix não pode ser nulo ou vazio");
        }

        if (config.DrEnabled is not (0 or 1))
        {
            errors.Add("DrEnabled deve ser 0 ou 1");
        }

        if (config.DrAllowCTGoSpec is not (0 or 1))
        {
            errors.Add("DrAllowCTGoSpec deve ser 0 ou 1");
        }

        if (config.DrOnlyDeathrunMaps is not (0 or 1))
        {
            errors.Add("DrOnlyDeathrunMaps deve ser 0 ou 1");
        }

        if (config.DrEnableBunnyhop is not (0 or 1))
        {
            errors.Add("DrEnableBunnyhop deve ser 0 ou 1");
        }

        if (config.DrVelocityMultiplierTR <= 0)
        {
            errors.Add("DrVelocityMultiplierTR deve ser maior que 0");
        }

        if (errors.Count > 0)
        {
            throw new Exception($"Erros de configuração: {string.Join(", ", errors)}");
        }
    }

    private void ApplyConfig(PluginConfig config)
    {
        _prefix = $"{ChatColors.Gold}[{ChatColors.Red}{config.DrPrefix}{ChatColors.Gold}]{ChatColors.Default}";
        _enabled = config.DrEnabled == 1;
        _allowCTGoSpec = config.DrAllowCTGoSpec == 1;
        _onlyDeathrunMaps = config.DrOnlyDeathrunMaps == 1;
        _enableBunnyhop = config.DrEnableBunnyhop == 1;
        _velocityMultiplierTR = config.DrVelocityMultiplierTR;

        // Aplicar configurações de bunnyhop
        ApplyBunnyhopSettings();

        // Verificar novamente se o plugin deve estar ativo após mudança de config
        CheckIfDeathrunMap();
    }

    private HookResult CommandListener_JoinTeam(CCSPlayerController? player, CommandInfo info)
    {
        if (!ShouldPluginBeActive() || !IsPlayerValid(player))
        {
            return HookResult.Continue;
        }

        int currentTeam = player!.PlayerPawn.Value!.TeamNum;

        // Se estiver no espectador, forçar para CT
        if (currentTeam == SPECTATOR_TEAM)
        {
            player.ChangeTeam(CsTeam.CounterTerrorist);
            return HookResult.Handled;
        }

        if (!int.TryParse(info.GetArg(1), out int targetTeam))
        {
            return HookResult.Continue;
        }

        // Permitir CT ir para espectador se configurado
        if (_allowCTGoSpec && currentTeam == COUNTER_TERRORIST_TEAM && targetTeam == SPECTATOR_TEAM)
        {
            return HookResult.Continue;
        }

        var (_, playersCT, playersTR) = GetTeamPlayers();

        // Impedir mudança de TR para CT se já houver CT
        if (playersCT.Count > 0 && currentTeam == TERRORIST_TEAM)
        {
            player.PrintToChat($"{_prefix} {ChatColors.LightRed}{Localizer["team.change"]}{ChatColors.Default}");
            return HookResult.Handled;
        }

        // Bloquear entrada em TR ou SPEC
        if (targetTeam is TERRORIST_TEAM or SPECTATOR_TEAM)
        {
            player.PrintToChat($"{_prefix} {ChatColors.Orange}{Localizer["team.limit"]}{ChatColors.Default}");
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    private HookResult CommandListener_BlockCommands(CCSPlayerController? player, CommandInfo info)
    {
        if (!IsPlayerValid(player))
        {
            return HookResult.Continue;
        }

        player!.PrintToChat($"{_prefix} {ChatColors.Red}{Localizer["blocked.command"]} {ChatColors.Yellow}{info.GetCommandString}{ChatColors.Default}");
        return HookResult.Stop;
    }

    #region Console Commands

    [ConsoleCommand("dr_enabled", "Habilitar ou desabilitar o plugin DR Manager")]
    [CommandHelper(minArgs: 1, usage: "[1/0]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnDrEnabledCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (int.TryParse(command.ArgString, out int value) && value is 0 or 1)
        {
            _enabled = value == 1;
            Server.PrintToChatAll($"{_prefix} Plugin {(_enabled ? $"{ChatColors.Green}habilitado" : $"{ChatColors.Red}desabilitado")}{ChatColors.Default}");
        }
        else
        {
            command.ReplyToCommand($"{ChatColors.Red}Uso: dr_enabled [0/1]{ChatColors.Default}");
        }
    }

    [ConsoleCommand("dr_prefix", "Alterar prefixo do plugin DR Manager")]
    [CommandHelper(minArgs: 1, usage: "[texto]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnDrPrefixCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!string.IsNullOrWhiteSpace(command.ArgString))
        {
            _prefix = $"{ChatColors.Gold}[{ChatColors.Red}{command.ArgString}{ChatColors.Gold}]{ChatColors.Default}";
            command.ReplyToCommand($"{ChatColors.Green}Prefixo alterado para: {_prefix}");
        }
        else
        {
            command.ReplyToCommand($"{ChatColors.Red}Prefixo não pode ser vazio{ChatColors.Default}");
        }
    }

    [ConsoleCommand("dr_velocity_multiplier_tr", "Alterar multiplicador de velocidade do terrorista")]
    [CommandHelper(minArgs: 1, usage: "[número]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnVelocityMultiplierCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (float.TryParse(command.ArgString, out float velocity) && velocity > 0)
        {
            _velocityMultiplierTR = velocity;
            command.ReplyToCommand($"{ChatColors.Green}Multiplicador de velocidade alterado para: {ChatColors.Yellow}{velocity}{ChatColors.Default}");
        }
        else
        {
            command.ReplyToCommand($"{ChatColors.Red}Valor deve ser um número maior que 0{ChatColors.Default}");
        }
    }

    [ConsoleCommand("dr_allow_ct_spec", "Permitir CT ir para espectador")]
    [CommandHelper(minArgs: 1, usage: "[1/0]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnAllowCTSpecCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (int.TryParse(command.ArgString, out int value) && value is 0 or 1)
        {
            _allowCTGoSpec = value == 1;
            command.ReplyToCommand($"CT pode ir para espectador: {(_allowCTGoSpec ? $"{ChatColors.Green}Sim" : $"{ChatColors.Red}Não")}{ChatColors.Default}");
        }
        else
        {
            command.ReplyToCommand($"{ChatColors.Red}Uso: dr_allow_ct_spec [0/1]{ChatColors.Default}");
        }
    }

    [ConsoleCommand("dr_only_deathrun_maps", "Ativar plugin apenas em mapas de deathrun (dr_ ou deathrun_)")]
    [CommandHelper(minArgs: 1, usage: "[1/0]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnDrOnlyDeathrunMapsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (int.TryParse(command.ArgString, out int value) && value is 0 or 1)
        {
            _onlyDeathrunMaps = value == 1;
            CheckIfDeathrunMap();
            command.ReplyToCommand($"Plugin ativo apenas em mapas de deathrun: {(_onlyDeathrunMaps ? $"{ChatColors.Green}Sim" : $"{ChatColors.Red}Não")}{ChatColors.Default}");

            if (_onlyDeathrunMaps && !_isDeathrunMap)
            {
                command.ReplyToCommand($"{ChatColors.Orange}Mapa atual não é de deathrun - plugin desativado{ChatColors.Default}");
            }
        }
        else
        {
            command.ReplyToCommand($"{ChatColors.Red}Uso: dr_only_deathrun_maps [0/1]{ChatColors.Default}");
        }
    }

    [ConsoleCommand("dr_enable_bunnyhop", "Habilitar ou desabilitar bunnyhop no servidor")]
    [CommandHelper(minArgs: 1, usage: "[1/0]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnDrEnableBunnyhopCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (int.TryParse(command.ArgString, out int value) && value is 0 or 1)
        {
            _enableBunnyhop = value == 1;
            ApplyBunnyhopSettings();
            command.ReplyToCommand($"Bunnyhop: {(_enableBunnyhop ? $"{ChatColors.Green}Habilitado" : $"{ChatColors.Red}Desabilitado")}{ChatColors.Default}");

            if (ShouldPluginBeActive())
            {
                Server.PrintToChatAll($"{_prefix} Bunnyhop foi {(_enableBunnyhop ? $"{ChatColors.Green}habilitado" : $"{ChatColors.Red}desabilitado")}{ChatColors.Default}");
            }
        }
        else
        {
            command.ReplyToCommand($"{ChatColors.Red}Uso: dr_enable_bunnyhop [0/1]{ChatColors.Default}");
        }
    }

    #endregion

    #region Game Event Handlers

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        // Remover armas de todos os CTs vivos no fim do round
        RemoveWeaponsFromAliveCTs();

        return SelectRandomTerrorist();
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (ShouldPluginBeActive() && _selectedTerrorist != null && IsPlayerValid(_selectedTerrorist))
        {
            _selectedTerrorist.PlayerPawn!.Value!.VelocityModifier = _velocityMultiplierTR;
        }
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
    {
        return SelectRandomTerrorist();
    }

    [GameEventHandler]
    public HookResult OnRoundAnnounceWarmup(EventRoundAnnounceWarmup @event, GameEventInfo info)
    {
        return SelectRandomTerrorist();
    }

    #endregion

    private void RemoveWeaponsFromAliveCTs()
    {
        try
        {
            var (_, playersCT, _) = GetTeamPlayers();

            foreach (var player in playersCT)
            {
                if (!IsPlayerValid(player))
                    continue;

                // Verificar se o jogador está vivo
                var playerPawn = player.PlayerPawn?.Value;
                if (playerPawn == null || !playerPawn.IsValid)
                    continue;

                // Verificar se está vivo usando Health > 0
                if (playerPawn.Health <= 0)
                    continue;

                // Remover todas as armas exceto a faca
                try
                {
                    player.RemoveWeapons();

                    // Dar apenas a faca de volta
                    player.GiveNamedItem(CsItem.Knife);

                    Console.WriteLine($"[DR Manager] Armas removidas do jogador CT vivo: {player.PlayerName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DR Manager] Erro ao remover armas do jogador {player.PlayerName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DR Manager] Erro geral ao remover armas dos CTs vivos: {ex.Message}");
        }
    }

    private void RemoveWeaponsOnTheGround()
    {
        try
        {
            // Abordagem alternativa mais simples e compatível
            var weaponEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("weapon_");

            foreach (var weapon in weaponEntities)
            {
                if (!weapon.IsValid || !weapon.DesignerName.StartsWith("weapon_"))
                    continue;

                // Verificar se a arma tem um dono (está sendo carregada)
                bool hasOwner = false;

                // Tentar acessar propriedades comuns para verificar se tem dono
                try
                {
                    // Se conseguimos acessar estas propriedades, pode ser uma arma carregada
                    if (weapon is CCSWeaponBase weaponBase)
                    {
                        var owner = weaponBase.OwnerEntity?.Value;
                        if (owner != null && owner.IsValid)
                        {
                            hasOwner = true;
                        }
                    }
                }
                catch
                {
                    // Se der erro ao acessar, assumir que não tem dono
                    hasOwner = false;
                }

                // Se não tem dono, remover
                if (!hasOwner)
                {
                    weapon.Remove();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DR Manager] Erro ao remover armas do chão: {ex.Message}");

            // Fallback: usar comando do servidor para limpar armas
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

    private HookResult SelectRandomTerrorist()
    {
        if (!ShouldPluginBeActive()) return HookResult.Continue;

        var (allPlayers, playersCT, _) = GetTeamPlayers();

        if (allPlayers.Count <= 1)
        {
            Server.PrintToChatAll($"{_prefix} {ChatColors.Orange}{Localizer["minimum.players"]}{ChatColors.Default}");
            return HookResult.Continue;
        }

        if (playersCT.Count == 0)
        {
            Server.PrintToChatAll($"{_prefix} {ChatColors.Red}Não há jogadores CT disponíveis{ChatColors.Default}");
            return HookResult.Continue;
        }

        RemoveWeaponsOnTheGround();
        ResetAllPlayersToCounterTerrorist(allPlayers);

        // Selecionar terrorista aleatório
        var random = new Random();
        int selectedIndex = random.Next(playersCT.Count);
        var selectedTerrorist = playersCT[selectedIndex];

        Server.PrintToChatAll($"{_prefix} {ChatColors.Green}{Localizer["random.terrorist"]} {ChatColors.Blue}{selectedTerrorist.PlayerName}{ChatColors.Default}");
        selectedTerrorist.ChangeTeam(CsTeam.Terrorist);

        _selectedTerrorist = selectedTerrorist;

        return HookResult.Continue;
    }

    private void ResetAllPlayersToCounterTerrorist(List<CCSPlayerController> players)
    {
        foreach (var player in players)
        {
            if (!IsPlayerValid(player)) continue;

            player.SwitchTeam(CsTeam.CounterTerrorist);
            player.PlayerPawn!.Value!.VelocityModifier = 1.0f;
            player.RemoveWeapons();
            player.GiveNamedItem(CsItem.Knife);
        }
    }

    private void OnMapStart(string mapName)
    {
        Console.WriteLine($"[DR Manager] Mapa carregado: {mapName}");
        CheckIfDeathrunMap();

        if (_onlyDeathrunMaps)
        {
            if (_isDeathrunMap)
            {
                Console.WriteLine($"[DR Manager] Mapa de deathrun detectado - plugin ativado");
                Server.PrintToChatAll($"{_prefix} {ChatColors.Green}Mapa de deathrun detectado - plugin ativado{ChatColors.Default}");
            }
            else
            {
                Console.WriteLine($"[DR Manager] Mapa não é de deathrun - plugin desativado");
                Server.PrintToChatAll($"{_prefix} {ChatColors.Orange}Plugin desativado - mapa não é de deathrun{ChatColors.Default}");
            }
        }
    }

    private void CheckIfDeathrunMap()
    {
        try
        {
            string currentMap = Server.MapName.ToLowerInvariant();
            _isDeathrunMap = _mapPrefixes.Any(prefix => currentMap.StartsWith(prefix));

            Console.WriteLine($"[DR Manager] Verificação de mapa: {currentMap} - É deathrun: {_isDeathrunMap}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DR Manager] Erro ao verificar mapa: {ex.Message}");
            _isDeathrunMap = false;
        }
    }

    private bool ShouldPluginBeActive()
    {
        // Se o plugin está desabilitado globalmente, não ativar
        if (!_enabled) return false;

        // Se a configuração exige apenas mapas de deathrun, verificar se é um
        if (_onlyDeathrunMaps && !_isDeathrunMap) return false;

        return true;
    }

    private void ApplyBunnyhopSettings()
    {
        try
        {
            if (_enableBunnyhop)
            {
                // Habilitar bunnyhop
                Server.ExecuteCommand("sv_enablebunnyhopping 1");
                Server.ExecuteCommand("sv_autobunnyhopping 1");
                Server.ExecuteCommand("sv_airaccelerate 1000");
                Server.ExecuteCommand("sv_air_max_wishspeed 30");
                Server.ExecuteCommand("sv_staminamax 0");
                Server.ExecuteCommand("sv_staminajumpcost 0");
                Server.ExecuteCommand("sv_staminalandcost 0");

                Console.WriteLine("[DR Manager] Bunnyhop habilitado");
            }
            else
            {
                // Desabilitar bunnyhop (valores padrão do CS2)
                Server.ExecuteCommand("sv_enablebunnyhopping 0");
                Server.ExecuteCommand("sv_autobunnyhopping 0");
                Server.ExecuteCommand("sv_airaccelerate 12");
                Server.ExecuteCommand("sv_air_max_wishspeed 30");
                Server.ExecuteCommand("sv_staminamax 80");
                Server.ExecuteCommand("sv_staminajumpcost 0.080000");
                Server.ExecuteCommand("sv_staminalandcost 0.050000");

                Console.WriteLine("[DR Manager] Bunnyhop desabilitado");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DR Manager] Erro ao aplicar configurações de bunnyhop: {ex.Message}");
        }
    }

    private static (List<CCSPlayerController> all, List<CCSPlayerController> ct, List<CCSPlayerController> tr) GetTeamPlayers()
    {
        var allPlayers = Utilities.GetPlayers()
            .Where(p => p.TeamNum != SPECTATOR_TEAM && p.IsValid && p.PlayerPawn.IsValid)
            .ToList();

        var playersCT = allPlayers.Where(p => p.TeamNum == COUNTER_TERRORIST_TEAM).ToList();
        var playersTR = allPlayers.Where(p => p.TeamNum == TERRORIST_TEAM).ToList();

        return (allPlayers, playersCT, playersTR);
    }

    private static bool IsPlayerValid(CCSPlayerController? player)
    {
        return player != null && player.IsValid && player.PlayerPawn.IsValid;
    }
}