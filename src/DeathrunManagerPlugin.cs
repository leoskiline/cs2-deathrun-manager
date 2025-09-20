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
            var message = noFallDamage ? "nao toma mais dano de queda" : "volta a tomar dano de queda";
            _logger.LogInfo($"Configuracao de dano de queda alterada: {(noFallDamage ? "desabilitado" : "habilitado")}");
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

        Console.WriteLine("[DR Manager] Configuracoes do servidor aplicadas");
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
        _logger.LogInfo("Configuracao carregada e validada");

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
            command.ReplyToCommand($"{ChatColors.Red}Prefixo n�o pode ser vazio{ChatColors.Default}");
            return;
        }

        _settings.SetPrefix(command.ArgString);
        command.ReplyToCommand($"{ChatColors.Green}Prefixo alterado para: {_settings.FormattedPrefix}");
    }

    [ConsoleCommand("dr_velocity_multiplier_tr", "Alterar multiplicador de velocidade do terrorista")]
    [CommandHelper(minArgs: 1, usage: "[numero]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnVelocityMultiplierCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!CommandParser.TryParsePositiveFloat(command.ArgString, out float velocity))
        {
            command.ReplyToCommand($"{ChatColors.Red}Valor deve ser um numero maior que 0{ChatColors.Default}");
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
        var status = allow ? $"{ChatColors.Green}Sim" : $"{ChatColors.Red}N�o";
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

        var status = onlyDeathrun ? $"{ChatColors.Green}Sim" : $"{ChatColors.Red}Nao";
        command.ReplyToCommand($"Plugin ativo apenas em mapas de deathrun: {status}{ChatColors.Default}");

        if (onlyDeathrun && !_gameState.IsDeathrunMap)
        {
            command.ReplyToCommand($"{ChatColors.Orange}Mapa atual nao e de deathrun - plugin desativado{ChatColors.Default}");
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
        if (ShouldPluginBeActive())
        {
            if (_gameState.SelectedTerrorist != null && PlayerValidator.IsValid(_gameState.SelectedTerrorist))
            {
                _gameState.SelectedTerrorist.PlayerPawn!.Value!.VelocityModifier = _settings.VelocityMultiplierTR;
            }

            GiveKnivesToCTsWithoutKnife();
        }
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info) => SelectRandomTerrorist();

    [GameEventHandler]
    public HookResult OnRoundAnnounceWarmup(EventRoundAnnounceWarmup @event, GameEventInfo info) => SelectRandomTerrorist();

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (!ShouldPluginBeActive())
            return HookResult.Continue;

        var player = @event.Userid;
        if (!PlayerValidator.IsValid(player))
            return HookResult.Continue;

        var teamName = player!.TeamNum == (int)CsTeam.CounterTerrorist ? "CT" : 
                      player.TeamNum == (int)CsTeam.Terrorist ? "TR" : "Unknown";
        
        Console.WriteLine($"[DR Manager] {teamName} {player.PlayerName} spawnou - garantindo apenas faca...");

        // Aguarda um tick para garantir que o spawn foi completado
        Server.NextFrame(() => 
        {
            if (PlayerValidator.IsValid(player))
            {
                if (player.TeamNum == (int)CsTeam.CounterTerrorist)
                {
                    GiveKnifeToSingleCT(player);
                }
                else if (player.TeamNum == (int)CsTeam.Terrorist)
                {
                    GiveKnifeToSingleTR(player);
                }
            }
        });

        return HookResult.Continue;
    }

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
            _logger.LogWarning("Selecao de terrorista falhou na validacao de jogadores");
            return HookResult.Continue;
        }

        var terroristSelector = new TerroristSelector(_settings);
        var selectedTerrorist = terroristSelector.SelectRandom(teamData.CounterTerrorists);

        if (selectedTerrorist == null)
        {
            _logger.LogError("Falha ao selecionar terrorista aleatorio");
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
            Server.PrintToChatAll($"{_settings.FormattedPrefix} {ChatColors.Red}Nao ha jogadores CT disponiveis{ChatColors.Default}");
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
        
        // Garantir que o terrorista tenha apenas faca
        Server.NextFrame(() => 
        {
            if (PlayerValidator.IsValid(selectedTerrorist) && selectedTerrorist.TeamNum == (int)CsTeam.Terrorist)
            {
                selectedTerrorist.RemoveWeapons();
                selectedTerrorist.GiveNamedItem(CsItem.Knife);
                Console.WriteLine($"[DR Manager] Terrorista {selectedTerrorist.PlayerName} equipado apenas com faca");
            }
        });
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
            var currentMap = Server.MapName;
            if (string.IsNullOrEmpty(currentMap))
            {
                Console.WriteLine("[DR Manager] Nome do mapa não disponível");
                _gameState.IsDeathrunMap = false;
                return;
            }

            var mapChecker = new MapChecker();
            _gameState.IsDeathrunMap = mapChecker.IsDeathrunMap(currentMap);

            Console.WriteLine($"[DR Manager] Verificação de mapa: {currentMap} - é deathrun: {_gameState.IsDeathrunMap}");
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
            Console.WriteLine("[DR Manager] Mapa nao e de deathrun - plugin desativado");
            Server.PrintToChatAll($"{_settings.FormattedPrefix} {ChatColors.Orange}Plugin desativado - mapa nao e de deathrun{ChatColors.Default}");
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
            _logger.LogError($"Erro ao aplicar configuracoes de movimento: {ex.Message}", ex);
            Console.WriteLine($"[DR Manager] Erro ao aplicar configuracoes de movimento: {ex.Message}");
        }
    }

    private void GiveKnivesToCTsWithoutKnife()
    {
        try
        {
            Console.WriteLine("[DR Manager] Removendo todas as armas dos CTs e dando apenas faca...");
            var teamData = TeamUtilities.GetTeamPlayers();
            
            Console.WriteLine($"[DR Manager] Encontrados {teamData.CounterTerrorists.Count} jogadores CT");
            
            foreach (var ct in teamData.CounterTerrorists.Where(PlayerValidator.IsValid))
            {
                try
                {
                    Console.WriteLine($"[DR Manager] Processando armas do CT: {ct.PlayerName}");
                    
                    if (ct.PlayerPawn?.Value == null) 
                    {
                        Console.WriteLine($"[DR Manager] ERRO: PlayerPawn nulo para CT {ct.PlayerName}");
                        continue;
                    }

                    // Remove todas as armas do jogador
                    ct.RemoveWeapons();
                    Console.WriteLine($"[DR Manager] Todas as armas removidas do CT: {ct.PlayerName}");
                    
                    // Dá apenas a faca
                    ct.GiveNamedItem(CsItem.Knife);
                    Console.WriteLine($"[DR Manager] Faca dada ao CT: {ct.PlayerName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DR Manager] ERRO ao verificar faca do CT {ct.PlayerName}: {ex.Message}");
                    Console.WriteLine($"[DR Manager] Stack trace: {ex.StackTrace}");
                    _logger.LogError($"Erro ao verificar faca do CT: {ex.Message}", ex);
                }
            }
            
            Console.WriteLine("[DR Manager] Verificação de facas dos CTs concluída");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DR Manager] ERRO GERAL na verificação de facas: {ex.Message}");
            Console.WriteLine($"[DR Manager] Stack trace: {ex.StackTrace}");
            _logger.LogError($"Erro geral na verificação de facas: {ex.Message}", ex);
        }
    }

    private void GiveKnifeToSingleCT(CCSPlayerController player)
    {
        try
        {
            Console.WriteLine($"[DR Manager] Garantindo apenas faca para CT: {player.PlayerName}");
            
            if (player.PlayerPawn?.Value == null) 
            {
                Console.WriteLine($"[DR Manager] ERRO: PlayerPawn nulo para CT {player.PlayerName}");
                return;
            }

            // Remove todas as armas e dá apenas faca
            player.RemoveWeapons();
            player.GiveNamedItem(CsItem.Knife);
            Console.WriteLine($"[DR Manager] CT {player.PlayerName} equipado apenas com faca");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DR Manager] ERRO ao dar faca individual para CT {player.PlayerName}: {ex.Message}");
            _logger.LogError($"Erro ao dar faca individual para CT: {ex.Message}", ex);
        }
    }

    private void GiveKnifeToSingleTR(CCSPlayerController player)
    {
        try
        {
            Console.WriteLine($"[DR Manager] Garantindo apenas faca para TR: {player.PlayerName}");
            
            if (player.PlayerPawn?.Value == null) 
            {
                Console.WriteLine($"[DR Manager] ERRO: PlayerPawn nulo para TR {player.PlayerName}");
                return;
            }

            // Remove todas as armas e dá apenas faca
            player.RemoveWeapons();
            player.GiveNamedItem(CsItem.Knife);
            Console.WriteLine($"[DR Manager] TR {player.PlayerName} equipado apenas com faca");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DR Manager] ERRO ao dar faca individual para TR {player.PlayerName}: {ex.Message}");
            _logger.LogError($"Erro ao dar faca individual para TR: {ex.Message}", ex);
        }
    }
    #endregion
}


public record TeamData(
    List<CCSPlayerController> AllPlayers,
    List<CCSPlayerController> CounterTerrorists,
    List<CCSPlayerController> Terrorists
);