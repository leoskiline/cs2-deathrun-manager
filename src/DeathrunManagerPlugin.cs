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
    public override string ModuleVersion => "0.5.0";
    public override string ModuleAuthor => "Psycho";
    public override string ModuleDescription => "Deathrun Manager plugin for CounterStrike Sharp";

    #region Constants
    private const int MINIMUM_PLAYERS = 2;

    private static readonly string[] BlockedCommands = { "kill", "killvector", "explodevector", "explode" };
    private static readonly string[] MapPrefixes = { "dr_", "deathrun_" };

    private static readonly Dictionary<string, string> BunnyhopSettings = new()
    {
        { "sv_enablebunnyhopping", "1" },
        { "sv_autobunnyhopping", "1" },
        { "sv_airaccelerate", "1000" },
        { "sv_air_max_wishspeed", "30" },
        { "sv_staminamax", "0" },
        { "sv_staminajumpcost", "0" },
        { "sv_staminalandcost", "0" }
    };

    private static readonly Dictionary<string, string> DefaultMovementSettings = new()
    {
        { "sv_enablebunnyhopping", "0" },
        { "sv_autobunnyhopping", "0" },
        { "sv_airaccelerate", "12" },
        { "sv_air_max_wishspeed", "30" },
        { "sv_staminamax", "80" },
        { "sv_staminajumpcost", "0.080000" },
        { "sv_staminalandcost", "0.050000" }
    };
    [ConsoleCommand("dr_terrorist_no_fall_damage", "Habilitar ou desabilitar dano de queda para terrorista")]
    [CommandHelper(minArgs: 1, usage: "[1/0]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnDrTerroristNoFallDamageCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!CommandParser.TryParseBool(command.ArgString, out bool noFallDamage))
        {
            command.ReplyToCommand($"{ChatColors.Red}Uso: dr_terrorist_no_fall_damage [0/1]{ChatColors.Default}");
            return;
        }

        _settings.TerroristNoFallDamage = noFallDamage;
        var status = noFallDamage ? $"{ChatColors.Green}Desabilitado" : $"{ChatColors.Red}Habilitado";
        command.ReplyToCommand($"Dano de queda para terrorista: {status}{ChatColors.Default}");

        if (ShouldPluginBeActive())
        {
            var message = noFallDamage ? "não toma mais dano de queda" : "volta a tomar dano de queda";
            _logger.LogInfo($"Configuração de dano de queda alterada: {(noFallDamage ? "desabilitado" : "habilitado")}");
        }
    }
    #endregion

    #region Private Fields
    private GameState _gameState = new();
    private PluginSettings _settings = new();
    private PluginLogger _logger = new();
    private bool _convarLoaded = false;
    #endregion

    public required PluginConfig Config { get; set; }

    #region Initialization
    public override void Load(bool hotReload)
    {
        InitializePlugin();
        RegisterEventListeners();
        CheckMapCompatibility();
    }

    private void InitializePlugin()
    {
        _logger.Initialize(_settings);
        _logger.LogInfo("Plugin inicializado");
        Console.WriteLine("[DR Manager] Plugin inicializado");
    }

    private void RegisterEventListeners()
    {
        RegisterListener<Listeners.OnServerPreWorldUpdate>(_ => OnServerPreWorldUpdate());
        RegisterListener<Listeners.OnMapStart>(OnMapStart);

        if (ShouldPluginBeActive())
        {
            RegisterCommandListeners();
        }
    }

    private void RegisterCommandListeners()
    {
        foreach (string command in BlockedCommands)
        {
            AddCommandListener(command, CommandListener_BlockCommands);
        }
        AddCommandListener("jointeam", CommandListener_JoinTeam);
    }

    private void OnServerPreWorldUpdate()
    {
        if (_convarLoaded) return;

        ApplyServerSettings();
        ApplyMovementSettings();
        _convarLoaded = true;

        Console.WriteLine("[DR Manager] Configurações do servidor aplicadas");
    }

    private void ApplyServerSettings()
    {
        var serverCommands = new[]
        {
            "mp_t_default_secondary 0",
            "mp_ct_default_secondary 0",
            "mp_autoteambalance 0",
            "mp_limitteams 0"
        };

        foreach (var command in serverCommands)
        {
            Server.ExecuteCommand(command);
        }
    }
    #endregion

    #region Configuration
    public void OnConfigParsed(PluginConfig config)
    {
        ValidateConfiguration(config);
        _settings.ApplyConfig(config);
        Config = config;

        _logger.Initialize(_settings);
        _logger.LogInfo("Configuração carregada e validada");

        ApplyMovementSettings();
        CheckMapCompatibility();
    }

    private static void ValidateConfiguration(PluginConfig config)
    {
        var validator = new ConfigValidator();
        validator.ValidateAndThrow(config);
    }
    #endregion

    #region Command Listeners
    private HookResult CommandListener_JoinTeam(CCSPlayerController? player, CommandInfo info)
    {
        if (!ShouldPluginBeActive() || !PlayerValidator.IsValid(player))
            return HookResult.Continue;

        var teamManager = new TeamManager(_settings);
        return teamManager.HandleTeamChange(player!, info);
    }

    private HookResult CommandListener_BlockCommands(CCSPlayerController? player, CommandInfo info)
    {
        if (!PlayerValidator.IsValid(player))
            return HookResult.Continue;

        player!.PrintToChat($"{_settings.FormattedPrefix} {ChatColors.Red}{Localizer["blocked.command"]} {ChatColors.Yellow}{info.GetCommandString}{ChatColors.Default}");
        return HookResult.Stop;
    }
    #endregion

    #region Console Commands
    [ConsoleCommand("dr_enabled", "Habilitar ou desabilitar o plugin DR Manager")]
    [CommandHelper(minArgs: 1, usage: "[1/0]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnDrEnabledCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!CommandParser.TryParseBool(command.ArgString, out bool enabled))
        {
            command.ReplyToCommand($"{ChatColors.Red}Uso: dr_enabled [0/1]{ChatColors.Default}");
            return;
        }

        _settings.Enabled = enabled;
        var status = enabled ? $"{ChatColors.Green}habilitado" : $"{ChatColors.Red}desabilitado";
        Server.PrintToChatAll($"{_settings.FormattedPrefix} Plugin {status}{ChatColors.Default}");
    }

    [ConsoleCommand("dr_prefix", "Alterar prefixo do plugin DR Manager")]
    [CommandHelper(minArgs: 1, usage: "[texto]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnDrPrefixCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (string.IsNullOrWhiteSpace(command.ArgString))
        {
            command.ReplyToCommand($"{ChatColors.Red}Prefixo não pode ser vazio{ChatColors.Default}");
            return;
        }

        _settings.SetPrefix(command.ArgString);
        command.ReplyToCommand($"{ChatColors.Green}Prefixo alterado para: {_settings.FormattedPrefix}");
    }

    [ConsoleCommand("dr_velocity_multiplier_tr", "Alterar multiplicador de velocidade do terrorista")]
    [CommandHelper(minArgs: 1, usage: "[número]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnVelocityMultiplierCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!CommandParser.TryParsePositiveFloat(command.ArgString, out float velocity))
        {
            command.ReplyToCommand($"{ChatColors.Red}Valor deve ser um número maior que 0{ChatColors.Default}");
            return;
        }

        _settings.VelocityMultiplierTR = velocity;
        command.ReplyToCommand($"{ChatColors.Green}Multiplicador de velocidade alterado para: {ChatColors.Yellow}{velocity}{ChatColors.Default}");
    }

    [ConsoleCommand("dr_allow_ct_spec", "Permitir CT ir para espectador")]
    [CommandHelper(minArgs: 1, usage: "[1/0]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnAllowCTSpecCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!CommandParser.TryParseBool(command.ArgString, out bool allow))
        {
            command.ReplyToCommand($"{ChatColors.Red}Uso: dr_allow_ct_spec [0/1]{ChatColors.Default}");
            return;
        }

        _settings.AllowCTGoSpec = allow;
        var status = allow ? $"{ChatColors.Green}Sim" : $"{ChatColors.Red}Não";
        command.ReplyToCommand($"CT pode ir para espectador: {status}{ChatColors.Default}");
    }

    [ConsoleCommand("dr_only_deathrun_maps", "Ativar plugin apenas em mapas de deathrun")]
    [CommandHelper(minArgs: 1, usage: "[1/0]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnDrOnlyDeathrunMapsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!CommandParser.TryParseBool(command.ArgString, out bool onlyDeathrun))
        {
            command.ReplyToCommand($"{ChatColors.Red}Uso: dr_only_deathrun_maps [0/1]{ChatColors.Default}");
            return;
        }

        _settings.OnlyDeathrunMaps = onlyDeathrun;
        CheckMapCompatibility();

        var status = onlyDeathrun ? $"{ChatColors.Green}Sim" : $"{ChatColors.Red}Não";
        command.ReplyToCommand($"Plugin ativo apenas em mapas de deathrun: {status}{ChatColors.Default}");

        if (onlyDeathrun && !_gameState.IsDeathrunMap)
        {
            command.ReplyToCommand($"{ChatColors.Orange}Mapa atual não é de deathrun - plugin desativado{ChatColors.Default}");
        }
    }

    [ConsoleCommand("dr_enable_bunnyhop", "Habilitar ou desabilitar bunnyhop no servidor")]
    [CommandHelper(minArgs: 1, usage: "[1/0]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnDrEnableBunnyhopCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!CommandParser.TryParseBool(command.ArgString, out bool enabled))
        {
            command.ReplyToCommand($"{ChatColors.Red}Uso: dr_enable_bunnyhop [0/1]{ChatColors.Default}");
            return;
        }

        _settings.EnableBunnyhop = enabled;
        ApplyMovementSettings();

        var status = enabled ? $"{ChatColors.Green}Habilitado" : $"{ChatColors.Red}Desabilitado";
        command.ReplyToCommand($"Bunnyhop: {status}{ChatColors.Default}");

        if (ShouldPluginBeActive())
        {
            Server.PrintToChatAll($"{_settings.FormattedPrefix} Bunnyhop foi {status}{ChatColors.Default}");
        }
    }
    #endregion

    #region Game Event Handlers
    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        var weaponManager = new WeaponManager();
        weaponManager.RemoveWeaponsFromAliveCTs();

        return SelectRandomTerrorist();
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (ShouldPluginBeActive() && _gameState.SelectedTerrorist != null &&
            PlayerValidator.IsValid(_gameState.SelectedTerrorist))
        {
            _gameState.SelectedTerrorist.PlayerPawn!.Value!.VelocityModifier = _settings.VelocityMultiplierTR;
        }
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info) => SelectRandomTerrorist();

    [GameEventHandler]
    public HookResult OnRoundAnnounceWarmup(EventRoundAnnounceWarmup @event, GameEventInfo info) => SelectRandomTerrorist();

    [GameEventHandler]
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        if (!ShouldPluginBeActive() || !_settings.TerroristNoFallDamage)
            return HookResult.Continue;

        var player = @event.Userid;
        if (!PlayerValidator.IsValid(player) || player!.TeamNum != (int)CsTeam.Terrorist)
            return HookResult.Continue;

        // Verifica se o dano foi causado por queda (fall damage)
        if (@event.Attacker == player && @event.Weapon == "worldspawn")
        {
            // Restaura a vida perdida pelo dano de queda
            var currentHealth = player.PlayerPawn!.Value!.Health;
            var damageAmount = @event.DmgHealth;
            var restoredHealth = Math.Min(currentHealth + damageAmount, 100);

            player.PlayerPawn.Value.Health = restoredHealth;

            _logger.LogDebug($"Dano de queda negado para terrorista: {player.PlayerName} (dano: {damageAmount})");
            Console.WriteLine($"[DR Manager] Dano de queda negado para terrorista: {player.PlayerName} (dano: {damageAmount})");
        }

        return HookResult.Continue;
    }
    #endregion

    #region Private Methods
    private HookResult SelectRandomTerrorist()
    {
        if (!ShouldPluginBeActive())
            return HookResult.Continue;

        var teamData = TeamUtilities.GetTeamPlayers();

        if (!ValidatePlayersForSelection(teamData))
        {
            _logger.LogWarning("Seleção de terrorista falhou na validação de jogadores");
            return HookResult.Continue;
        }

        var terroristSelector = new TerroristSelector(_settings);
        var selectedTerrorist = terroristSelector.SelectRandom(teamData.CounterTerrorists);

        if (selectedTerrorist == null)
        {
            _logger.LogError("Falha ao selecionar terrorista aleatório");
            return HookResult.Continue;
        }

        ApplyTerroristSelection(teamData.AllPlayers, selectedTerrorist);
        _gameState.SelectedTerrorist = selectedTerrorist;

        _logger.LogInfo($"Terrorista selecionado: {selectedTerrorist.PlayerName}");

        return HookResult.Continue;
    }

    private bool ValidatePlayersForSelection(TeamData teamData)
    {
        if (teamData.AllPlayers.Count < MINIMUM_PLAYERS)
        {
            Server.PrintToChatAll($"{_settings.FormattedPrefix} {ChatColors.Orange}{Localizer["minimum.players"]}{ChatColors.Default}");
            return false;
        }

        if (teamData.CounterTerrorists.Count == 0)
        {
            Server.PrintToChatAll($"{_settings.FormattedPrefix} {ChatColors.Red}Não há jogadores CT disponíveis{ChatColors.Default}");
            return false;
        }

        return true;
    }

    private void ApplyTerroristSelection(List<CCSPlayerController> allPlayers, CCSPlayerController selectedTerrorist)
    {
        var weaponManager = new WeaponManager();
        weaponManager.RemoveWeaponsOnTheGround();

        ResetAllPlayersToCounterTerrorist(allPlayers);

        Server.PrintToChatAll($"{_settings.FormattedPrefix} {ChatColors.Green}{Localizer["random.terrorist"]} {ChatColors.Blue}{selectedTerrorist.PlayerName}{ChatColors.Default}");
        selectedTerrorist.ChangeTeam(CsTeam.Terrorist);
    }

    private void ResetAllPlayersToCounterTerrorist(List<CCSPlayerController> players)
    {
        foreach (var player in players.Where(PlayerValidator.IsValid))
        {
            player.SwitchTeam(CsTeam.CounterTerrorist);
            player.PlayerPawn!.Value!.VelocityModifier = 1.0f;
            player.RemoveWeapons();
            player.GiveNamedItem(CsItem.Knife);
        }
    }

    private void OnMapStart(string mapName)
    {
        _logger.LogInfo($"Mapa carregado: {mapName}");
        Console.WriteLine($"[DR Manager] Mapa carregado: {mapName}");
        CheckMapCompatibility();
        NotifyMapStatus();
    }

    private void CheckMapCompatibility()
    {
        try
        {
            var mapChecker = new MapChecker();
            _gameState.IsDeathrunMap = mapChecker.IsDeathrunMap(Server.MapName);

            Console.WriteLine($"[DR Manager] Verificação de mapa: {Server.MapName} - É deathrun: {_gameState.IsDeathrunMap}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao verificar mapa: {ex.Message}", ex);
            Console.WriteLine($"[DR Manager] Erro ao verificar mapa: {ex.Message}");
            _gameState.IsDeathrunMap = false;
        }
    }

    private void NotifyMapStatus()
    {
        if (!_settings.OnlyDeathrunMaps) return;

        if (_gameState.IsDeathrunMap)
        {
            Console.WriteLine("[DR Manager] Mapa de deathrun detectado - plugin ativado");
            Server.PrintToChatAll($"{_settings.FormattedPrefix} {ChatColors.Green}Mapa de deathrun detectado - plugin ativado{ChatColors.Default}");
        }
        else
        {
            Console.WriteLine("[DR Manager] Mapa não é de deathrun - plugin desativado");
            Server.PrintToChatAll($"{_settings.FormattedPrefix} {ChatColors.Orange}Plugin desativado - mapa não é de deathrun{ChatColors.Default}");
        }
    }

    private bool ShouldPluginBeActive()
    {
        return _settings.Enabled && (!_settings.OnlyDeathrunMaps || _gameState.IsDeathrunMap);
    }

    private void ApplyMovementSettings()
    {
        try
        {
            var settingsToApply = _settings.EnableBunnyhop ? BunnyhopSettings : DefaultMovementSettings;

            foreach (var (command, value) in settingsToApply)
            {
                Server.ExecuteCommand($"{command} {value}");
            }

            var status = _settings.EnableBunnyhop ? "habilitado" : "desabilitado";
            Console.WriteLine($"[DR Manager] Bunnyhop {status}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao aplicar configurações de movimento: {ex.Message}", ex);
            Console.WriteLine($"[DR Manager] Erro ao aplicar configurações de movimento: {ex.Message}");
        }
    }
    #endregion
}

#region Helper Classes
public class GameState
{
    public CCSPlayerController? SelectedTerrorist { get; set; }
    public bool IsDeathrunMap { get; set; }
}

public class PluginSettings
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

public static class PlayerValidator
{
    public static bool IsValid(CCSPlayerController? player)
    {
        return player?.IsValid == true && player.PlayerPawn?.IsValid == true;
    }
}

public static class CommandParser
{
    public static bool TryParseBool(string input, out bool result)
    {
        result = false;
        return int.TryParse(input, out int value) && value is 0 or 1 && (result = value == 1) || value == 0;
    }

    public static bool TryParsePositiveFloat(string input, out float result)
    {
        return float.TryParse(input, out result) && result > 0;
    }
}

public class ConfigValidator
{
    public void ValidateAndThrow(PluginConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.DrPrefix))
            errors.Add("DrPrefix não pode ser nulo ou vazio");

        if (config.DrEnabled is not (0 or 1))
            errors.Add("DrEnabled deve ser 0 ou 1");

        if (config.DrAllowCTGoSpec is not (0 or 1))
            errors.Add("DrAllowCTGoSpec deve ser 0 ou 1");

        if (config.DrOnlyDeathrunMaps is not (0 or 1))
            errors.Add("DrOnlyDeathrunMaps deve ser 0 ou 1");

        if (config.DrEnableBunnyhop is not (0 or 1))
            errors.Add("DrEnableBunnyhop deve ser 0 ou 1");

        if (config.DrTerroristNoFallDamage is not (0 or 1))
            errors.Add("DrTerroristNoFallDamage deve ser 0 ou 1");

        if (config.DrEnableDetailedLogging is not (0 or 1))
            errors.Add("DrEnableDetailedLogging deve ser 0 ou 1");

        if (config.DrLogRetentionDays < 1 || config.DrLogRetentionDays > 365)
            errors.Add("DrLogRetentionDays deve ser entre 1 e 365");

        if (config.DrVelocityMultiplierTR <= 0)
            errors.Add("DrVelocityMultiplierTR deve ser maior que 0");

        if (errors.Count > 0)
            throw new Exception($"Erros de configuração: {string.Join(", ", errors)}");
    }
}

public class MapChecker
{
    private static readonly string[] MapPrefixes = { "dr_", "deathrun_" };

    public bool IsDeathrunMap(string mapName)
    {
        var normalizedMapName = mapName.ToLowerInvariant();
        return MapPrefixes.Any(prefix => normalizedMapName.StartsWith(prefix));
    }
}

public class TeamManager
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

public record TeamData(
    List<CCSPlayerController> AllPlayers,
    List<CCSPlayerController> CounterTerrorists,
    List<CCSPlayerController> Terrorists
);

public static class TeamUtilities
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

public class TerroristSelector
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

public class WeaponManager
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

public class PluginLogger
{
    private PluginSettings? _settings;
    private readonly object _lockObject = new();
    private string _logDirectory = string.Empty;

    public void Initialize(PluginSettings settings)
    {
        _settings = settings;
        _logDirectory = Path.Combine(Server.GameDirectory, "csgo", "logs", "deathrun_manager");

        try
        {
            Directory.CreateDirectory(_logDirectory);
            CleanOldLogs();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DR Manager] Erro ao inicializar sistema de logs: {ex.Message}");
        }
    }

    public void LogInfo(string message)
    {
        WriteLog("INFO", message);
    }

    public void LogWarning(string message)
    {
        WriteLog("WARN", message);
    }

    public void LogError(string message, Exception? exception = null)
    {
        var fullMessage = exception != null ? $"{message} - Exception: {exception}" : message;
        WriteLog("ERROR", fullMessage);
    }

    public void LogDebug(string message)
    {
        if (_settings?.EnableDetailedLogging == true)
        {
            WriteLog("DEBUG", message);
        }
    }

    private void WriteLog(string level, string message)
    {
        if (_settings?.EnableDetailedLogging != true && level == "DEBUG")
            return;

        lock (_lockObject)
        {
            try
            {
                var logFileName = $"dr_manager_{DateTime.Now:yyyy-MM-dd}.log";
                var logFilePath = Path.Combine(_logDirectory, logFileName);
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

                File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DR Manager] Erro ao escrever log: {ex.Message}");
            }
        }
    }

    public void CleanOldLogs()
    {
        if (_settings == null || string.IsNullOrEmpty(_logDirectory))
            return;

        try
        {
            var cutoffDate = DateTime.Now.AddDays(-_settings.LogRetentionDays);
            var logFiles = Directory.GetFiles(_logDirectory, "dr_manager_*.log");

            foreach (var logFile in logFiles)
            {
                var fileInfo = new FileInfo(logFile);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    File.Delete(logFile);
                    Console.WriteLine($"[DR Manager] Log antigo removido: {fileInfo.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DR Manager] Erro ao limpar logs antigos: {ex.Message}");
        }
    }
}
#endregion