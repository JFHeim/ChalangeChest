using ChallengeChest.Patch;
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
            "", args => RunCommand(args =>
            {
                if (!IsAdmin) throw new ConsoleCommandException("You are not an admin on this server");

                EventSpawn.SpawnBoss();

                args.Context.AddString("Done");
            }, args), true);

        new Terminal.ConsoleCommand("finishChallenge",
            "Finish the Challenge Chest in the current player zone/chunk/sector", args => RunCommand(args =>
            {
                if (!IsAdmin) throw new ConsoleCommandException("You are not an admin on this server");

                EventSpawn.HandleChallengeDone(m_localPlayer.transform.position.GetZone());

                args.Context.AddString("Done");
            }, args), true);
    }
}