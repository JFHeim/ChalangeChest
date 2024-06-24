// ReSharper disable VariableHidesOuterVariable

using ChallengeChest.Patch;
using HarmonyLib;

namespace ChallengeChest;

[HarmonyPatch]
public static class TerminalCommands
{
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))] [HarmonyPostfix]
    private static void AddCommands()
    {
        new Terminal.ConsoleCommand("finishChallenge",
            "", args => RunCommand(args =>
            {
                if (!IsAdmin) throw new ConsoleCommandException("You are not an admin on this server");
                
                
                
                args.Context.AddString("Done");
            }, args), true);
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
file static class RemoveOnDeath
{
    private static void Prefix(Character __instance)
    {
        if (__instance.m_nview.GetZDO().GetLong("ChallengeChestTime") <= 0) return;
        __instance.m_nview.GetZDO().GetVec3("ChallengeChestSpawnPosition", out var spawnPos);
        var sector = ZoneSystem.instance.GetZone(spawnPos);
        if (ZNet.instance.IsServer()) InvokeEventPeriodically.HandleChallengeDone(sector);
        else ZNet.instance.GetServerPeer().m_rpc.Invoke("ChallengeChestDone", sector.x, sector.y);
    }
}