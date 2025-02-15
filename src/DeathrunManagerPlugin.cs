
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace DeathrunManagerPlugin;
public class DeathrunManagerPlugin : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "Deathrun Manager Plugin";

    public override string ModuleVersion => "0.1.0";
    
    public override string ModuleAuthor => "Psycho";

    public override string ModuleDescription => "Deathrun Manager plugin";

    private bool b_Enabled = true;
    private bool b_DeathrunAllowCTGoSpec = true;
    private float deathrunVelocityMultiplierTR = (float)1.75;
    private string prefix = "[DR Manager]";
    private string[] BlockedCommands = new string[] { "kill" ,"killvector","explodevector","explode"};
    private CCSPlayerController? selectedPlayerToTerrorist;
    private bool loadedConvar = false;

    private const int CT = 3;
    private const int TR = 2;
    private const int SPEC = 1;

    public override void Load(bool hotReload)
    {

        RegisterListener<Listeners.OnServerPreWorldUpdate>(simulating =>
        {
            OnServerPreWorldUpdate();
        });

        for (int i = 0; i < BlockedCommands.Length; i++)
        {
            AddCommandListener(BlockedCommands[i], CommandListener_BlockCommands);
        }

        AddCommandListener("jointeam", CommandListener_JoinTeam);
    }

    public required PluginConfig Config { get; set; }

    private void OnServerPreWorldUpdate()
    {
        if (!this.loadedConvar)
        {
            Console.WriteLine("[DR Manager] Convar config.");
            Server.ExecuteCommand("mp_t_default_secondary 0");
            Server.ExecuteCommand("mp_ct_default_secondary 0");
            Server.ExecuteCommand("mp_autoteambalance 0");
            Server.ExecuteCommand("mp_limitteams 0");
            this.loadedConvar = true;
        }
    }

    public void OnConfigParsed(PluginConfig config)
    {
        string errorMessage = "";
        


        if(!(config.DrPrefix is string))
        {
            errorMessage.Concat("DeathrunPrefix on config must be string,");
        }

        if(config.DrEnabled != 1 || config.DrEnabled != 0)
        {
            errorMessage.Concat("DeathrunEnabled on config must be 0 or 1,");
        }

        if (config.DrAllowCTGoSpec != 1 || config.DrAllowCTGoSpec != 0)
        {
            errorMessage.Concat("DeathrunAllowCTGoSpec on config must be 0 or 1,");
        }

        if(config.DrVelocityMultiplierTR <= 0)
        {
            errorMessage.Concat("DeathrunVelocityMultiplierTR on config file must be > 0");
        }


        if (errorMessage != "")
        {
            errorMessage.TrimEnd(',');
            throw new Exception(errorMessage);
        }

        prefix = config.DrPrefix;
        b_Enabled = config.DrEnabled == 1;
        b_DeathrunAllowCTGoSpec = config.DrAllowCTGoSpec == 1;
        deathrunVelocityMultiplierTR = config.DrVelocityMultiplierTR;

        Config = config;
    }

    private HookResult CommandListener_JoinTeam(CCSPlayerController? player, CommandInfo info)
    {
        if(b_Enabled != true)
        {
            return HookResult.Continue;
        }

        if (!player!.IsValid)
        {
            return HookResult.Continue;
        }

        if (!player.PlayerPawn.IsValid)
        {
            return HookResult.Continue;
        }

        if(player.PlayerPawn.Value!.TeamNum == SPEC)
        {
            player.ChangeTeam(CsTeam.CounterTerrorist);
            return HookResult.Handled;
        }

        int arg = Convert.ToInt32(info.GetArg(1));

        if(b_DeathrunAllowCTGoSpec && player.PlayerPawn.Value!.TeamNum == CT && arg == SPEC)
        {
            return HookResult.Continue;
        }


        List<CCSPlayerController> players, playersCT, playersTR;
        getPlayers(out players, out playersCT, out playersTR);
        if(playersCT.Count > 0 && player.PlayerPawn.Value.TeamNum == TR)
        {
            player.PrintToChat($"{prefix} {Localizer["team.change"]}");
            return HookResult.Handled;
        }

        if(arg == TR || arg == SPEC)
        {
            player.PrintToChat($"{prefix} {Localizer["team.limit"]}");
            return HookResult.Handled;
        }

        

        return HookResult.Continue;
    }

    private HookResult CommandListener_BlockCommands(CCSPlayerController? player, CommandInfo info)
    {
        if (!player!.IsValid)
        {
            return HookResult.Continue;
        }

        if (!player.PlayerPawn.IsValid)
        {
            return HookResult.Continue;
        }

        player.PrintToChat($"{prefix} {Localizer["blocked.command"]} {info.GetCommandString}");

        return HookResult.Stop;
    }

    [ConsoleCommand("dr_enabled", "Enable or Disable DR Manager Plugin.")]
    [CommandHelper(minArgs: 1,usage: "[1/0]", whoCanExecute:CommandUsage.CLIENT_AND_SERVER)]
    public void OnCommand(CCSPlayerController? player, CommandInfo command)
    {
        b_Enabled = Convert.ToBoolean(command.ArgString);
    }

    [ConsoleCommand("dr_prefix", "Change prefix DR Manager PLugin")]
    [CommandHelper(minArgs: 1, usage: "string", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void onDrPrefix(CCSPlayerController? player, CommandInfo command)
    {
        prefix = command.ArgString;
    }

    [ConsoleCommand("dr_velocity_multiplier_tr", "Change velocity multiplier from terrorist team")]
    [CommandHelper(minArgs: 1, usage: "float", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void onVelocityMultiplierTR(CCSPlayerController? player, CommandInfo command)
    {
        float velocity;
        float.TryParse(command.ArgString, out velocity);
        deathrunVelocityMultiplierTR = velocity;
    }

    [ConsoleCommand("dr_allow_ct_spec", "Allow CT change team to Spectator")]
    [CommandHelper(minArgs: 1, usage: "[1/0]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void onAllowCTSpec(CCSPlayerController? player, CommandInfo command)
    {
        b_DeathrunAllowCTGoSpec = Convert.ToBoolean(command.ArgString);
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event,GameEventInfo info)
    {
        return selectRandomTerrorist();
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event,GameEventInfo info)
    {
        if (b_Enabled && this.selectedPlayerToTerrorist != null)
        {
            this.selectedPlayerToTerrorist!.PlayerPawn!.Value!.VelocityModifier = (float)Config.DrVelocityMultiplierTR;
        }
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnWarmupEnd(EventWarmupEnd @event,GameEventInfo info)
    {
        return selectRandomTerrorist();
    }

    [GameEventHandler]
    public HookResult OnRoundAnnounceWarmup(EventRoundAnnounceWarmup @event, GameEventInfo info)
    {
        return selectRandomTerrorist();
    }

    private void RemoveWeaponsOnTheGround()
    {
        var entities = Utilities.FindAllEntitiesByDesignerName<CCSWeaponBaseGun>("weapon_");

        foreach (var entity in entities)
        {
            if (!entity.IsValid)
            {
                continue;
            }

            if (entity.State != CSWeaponState_t.WEAPON_NOT_CARRIED)
            {
                continue;
            }

            if (entity.DesignerName.StartsWith("weapon_") == false)
            {
                continue;
            }

            entity.Remove();
        }
    }

    private HookResult selectRandomTerrorist()
    {
        if(!b_Enabled) { return HookResult.Continue; };

        List<CCSPlayerController> players, playersCT, playersTR;
        getPlayers(out players, out playersCT, out playersTR);

        if (b_Enabled && players.Count <= 1)
        {
            Server.PrintToChatAll($"{prefix} {Localizer["minimum.players"]}");
            return HookResult.Continue;
        }

        var rand = new Random();
        int playersCTCount = playersCT.Count - 1;
        int sortedPlayerIndex = rand.Next(playersCTCount);
        

        RemoveWeaponsOnTheGround();

        players.ForEach(p =>
        {
            p.SwitchTeam(CsTeam.CounterTerrorist);
            p!.PlayerPawn!.Value!.VelocityModifier = 1;
            p.RemoveWeapons();
            p.GiveNamedItem(CounterStrikeSharp.API.Modules.Entities.Constants.CsItem.Knife);
        });

        CCSPlayerController selectedPlayerToTerrorist = playersCT[sortedPlayerIndex];
        Server.PrintToChatAll($"{prefix} {Localizer["random.terrorist"]} {selectedPlayerToTerrorist.PlayerName}");
        selectedPlayerToTerrorist!.ChangeTeam(CsTeam.Terrorist);
        this.selectedPlayerToTerrorist = selectedPlayerToTerrorist;
       
        return HookResult.Continue;
    }

    private static void getPlayers(out List<CCSPlayerController> players, out List<CCSPlayerController> playersCT, out List<CCSPlayerController> playersTR)
    {
        players = Utilities.GetPlayers().Where(s => s.TeamNum != SPEC).ToList();
        playersCT = players.Where(s => s.TeamNum == CT).ToList();
        playersTR = players.Where(s => s.TeamNum == TR).ToList();
    }
}
