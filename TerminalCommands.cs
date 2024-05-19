// ReSharper disable VariableHidesOuterVariable

using ChalangeChest.Patch;
using HarmonyLib;

namespace ChalangeChest;

[HarmonyPatch]
public static class TerminalCommands
{
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))] [HarmonyPostfix]
    private static void AddCommands()
    {
        new Terminal.ConsoleCommand("deleteAllChalangeChests",
            "", args => RunCommand(args =>
            {
                if (!IsAdmin) throw new ConsoleCommandException("You are not an admin on this server");
                currentEvents.Clear();
                InvokeEventPeriodically.UpdateSyncData();
                ShowOnMap.needsUpdate = true;
                
                args.Context.AddString("Done");
            }, args), true);
    }
}