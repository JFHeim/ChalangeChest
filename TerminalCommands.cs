using HarmonyLib;

// ReSharper disable VariableHidesOuterVariable ObjectCreationAsStatement

namespace ChallengeChest;

[HarmonyPatch]
public static class TerminalCommands
{
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    [HarmonyPostfix]
    private static void AddCommands()
    {
        new Terminal.ConsoleCommand(ModName,
            "Info", args => RunCommand(args =>
            {
                args.Context.AddString($"{ModName} by {ModAuthor} v{ModVersion}");
                args.Context.AddString($"Available commands:");

                foreach (var commandName in new List<string>
                         {
                             ModName.ToLower(), "startchallenge", "startchallengehere", "finishchallengehere",
                             "finishchallengeall"
                         })
                {
                    var command = Terminal.commands[commandName.ToLower()];
                    args.Context.AddString($"\t{commandName.ToLower()} - {command.Description}");
                }

                args.Context.AddString("Done");
            }, args), true);

        new Terminal.ConsoleCommand("startchallenge",
            "Start the Challenge Chest at randomly generated position", args => RunCommand(args =>
            {
                if (!IsAdmin) throw new ConsoleCommandException("You are not an admin on this server");

                ZRoutedRpc.instance.InvokeRoutedRPC("cc_SpawnBossTerminalNoPos");

                args.Context.AddString("Done");
            }, args), true);

        new Terminal.ConsoleCommand("startchallengehere",
            "Start the Challenge Chest at current player position", args => RunCommand(args =>
            {
                if (!IsAdmin) throw new ConsoleCommandException("You are not an admin on this server");

                var pos = m_localPlayer.transform.position.RoundCords();
                ZRoutedRpc.instance.InvokeRoutedRPC("cc_SpawnBossTerminal", (double)pos.x, (double)pos.z);

                args.Context.AddString("Done");
            }, args), true);

        new Terminal.ConsoleCommand("finishchallengeall",
            "Finish all Challenge Chests in world", args => RunCommand(async args =>
            {
                if (!IsAdmin) throw new ConsoleCommandException("You are not an admin on this server");

                while (EventSpawn.EventDatas.Count > 0)
                {
                    var data = EventSpawn.EventDatas.FirstOrDefault();
                    if (data is null) break;

                    args.Context.AddString($"Processing event \"{data}\"");

                    var eventPos = data.pos.ToVector2();
                    ZRoutedRpc.instance.InvokeRoutedRPC("cc_HandleChallengeDone", (double)eventPos.x,
                        (double)eventPos.y);
                    var oldCount = EventSpawn.EventDatas.Count;
                    do await Task.Delay(100);
                    while (EventSpawn.EventDatas.Count == oldCount);
                }

                args.Context.AddString("Done");
            }, args), true);

        new Terminal.ConsoleCommand("finishchallengehere",
            "Finish the Challenge Chest in the current player zone/chunk/sector", args => RunCommand(args =>
            {
                if (!IsAdmin) throw new ConsoleCommandException("You are not an admin on this server");

                var pos = m_localPlayer.transform.position.ToV2();
                ZRoutedRpc.instance.InvokeRoutedRPC("cc_HandleChallengeDone", (double)pos.x, (double)pos.y);

                args.Context.AddString("Done");
            }, args), true);
    }
}