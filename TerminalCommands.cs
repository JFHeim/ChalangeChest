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
        new Terminal.ConsoleCommand("startChallenge",
            "Start the Challenge Chest at randomly generated position", args => RunCommand(args =>
            {
                if (!IsAdmin) throw new ConsoleCommandException("You are not an admin on this server");

                EventSpawn.SpawnBoss();

                args.Context.AddString("Done");
            }, args), true);

        new Terminal.ConsoleCommand("startChallengehere",
            "Start the Challenge Chest at current player position", args => RunCommand(args =>
            {
                if (!IsAdmin) throw new ConsoleCommandException("You are not an admin on this server");

                EventSpawn.SpawnBoss(m_localPlayer.transform.position.RoundCords());

                args.Context.AddString("Done");
            }, args), true);

        new Terminal.ConsoleCommand("finishChallengehere",
            "Finish the Challenge Chest in the current player zone/chunk/sector", args => RunCommand(args =>
            {
                if (!IsAdmin) throw new ConsoleCommandException("You are not an admin on this server");

                EventSpawn.HandleChallengeDone(m_localPlayer.transform.position.ToV2());

                args.Context.AddString("Done");
            }, args), true);
    }
}