
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace DeathrunManagerPlugin;
public class DeathrunManagerPlugin : BasePlugin
{
    public override string ModuleName => "Deathrun Manager Plugin";

    public override string ModuleVersion => "0.0.2";
    
    public override string ModuleAuthor => "Psycho";

    public override string ModuleDescription => "Deathrun Manager plugin";

    private bool b_Enabled = true;
    private string prefix = "[DR Manager]";
    private string[] BlockedCommands = new string[] { "kill" ,"killvector","explodevector","explode"};

    private const int CT = 3;
    private const int TR = 2;
    private const int SPEC = 1;

    public override void Load(bool hotReload)
    {
        for (int i = 0; i < BlockedCommands.Length; i++)
        {
            AddCommandListener(BlockedCommands[i], CommandListener_BlockCommands);
        }
        Server.ExecuteCommand("mp_t_default_secondary 0");
        Server.ExecuteCommand("mp_ct_default_secondary 0");

        AddCommandListener("jointeam", CommandListener_JoinTeam);
    }

    private HookResult CommandListener_JoinTeam(CCSPlayerController? player, CommandInfo info)
    {
        if(b_Enabled != true)
        {
            return HookResult.Continue;
        }

        if (!player.IsValid)
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

        List<CCSPlayerController> players, playersCT, playersTR;
        getPlayers(out players, out playersCT, out playersTR);
        if(playersCT.Count > 0 && player.PlayerPawn.Value.TeamNum == TR)
        {
            player.PrintToChat($"{TextColor.Green}{prefix} {TextColor.Default} You cannot change team.");
            return HookResult.Handled;
        }

        if(arg == TR || arg == SPEC)
        {
            player.PrintToChat($"{TextColor.Green}{prefix} {TextColor.Default} Team limit reached.");
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

        player.PrintToChat($"{TextColor.Green}{prefix} {TextColor.Default} Blocked command: {info.GetCommandString}");

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

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event,GameEventInfo info)
    {
        return selectRandomTerrorist();
    }

    [GameEventHandler]
    public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo info)
    {
        if(@event.Userid!.IsValid){
            Server.PrintToChatAll($"{TextColor.Green}{prefix} {TextColor.Default}{@event.Userid.PlayerName} connected!");
        }
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerConnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if(@event.Userid!.IsValid){
            Server.PrintToChatAll($"{TextColor.Green}{prefix} {TextColor.Default}{@event.Userid.PlayerName} disconnected!");
        }
        return HookResult.Continue;
    }


    private HookResult selectRandomTerrorist()
    {
        if(!b_Enabled) { return HookResult.Continue; };
        List<CCSPlayerController> players, playersCT, playersTR;
        getPlayers(out players, out playersCT, out playersTR);

        if (b_Enabled && players.Count <= 1)
        {
            Server.PrintToChatAll($"{TextColor.Green}{prefix} {TextColor.Default}Minimum 2 players required to start DR.");
            return HookResult.Continue;
        }
        

        var rand = new Random();
        int playersCTCount = playersCT.Count - 1;
        int sortedPlayerIndex = rand.Next(playersCTCount);
        CCSPlayerController selectedPlayerToTerrorist = playersCT[sortedPlayerIndex];

        if (playersTR.Count > 0)
        {
            CCSPlayerController selectPlayerOnTerrorist = playersTR.First();
            selectPlayerOnTerrorist.ChangeTeam(CsTeam.CounterTerrorist);
        }
        
        players.ForEach(p => p.ChangeTeam(CsTeam.CounterTerrorist));
        Server.PrintToChatAll($"{TextColor.Green}{prefix} {TextColor.Default}New Random Terrorist Selected: {selectedPlayerToTerrorist.PlayerName}");
        selectedPlayerToTerrorist!.ChangeTeam(CsTeam.Terrorist);
        return HookResult.Continue;
    }

    private static void getPlayers(out List<CCSPlayerController> players, out List<CCSPlayerController> playersCT, out List<CCSPlayerController> playersTR)
    {
        players = Utilities.GetPlayers().Where(s => s.IsValid).Where(s => s.PlayerPawn.Value != null).ToList();
        playersCT = players.Where(s => s.TeamNum == CT).ToList();
        playersTR = players.Where(s => s.TeamNum == TR).ToList();
    }
}
