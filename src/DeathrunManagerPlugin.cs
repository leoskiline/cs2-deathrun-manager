using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using DeathrunManager;

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


public record TeamData(
    List<CCSPlayerController> AllPlayers,
    List<CCSPlayerController> CounterTerrorists,
    List<CCSPlayerController> Terrorists
);