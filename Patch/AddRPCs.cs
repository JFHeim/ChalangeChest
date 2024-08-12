using HarmonyLib;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
[HarmonyWrapSafe]
file static class AddRPCs
{
    [UsedImplicitly, HarmonyPostfix]
    private static void Postfix()
    {
        ZRoutedRpc.instance.Register<ZPackage>("cc_SyncEventData", EventSpawn.ReceiveEventData);
        ZRoutedRpc.instance.Register<double, double>("cc_HandleChallengeDone", EventSpawn.HandleChallengeDone);
        ZRoutedRpc.instance.Register("cc_SpawnBossTerminalNoPos", EventSpawn.SpawnBoss);
        ZRoutedRpc.instance.Register<double, double>("cc_SpawnBossTerminal", EventSpawn.SpawnBoss);
        // ZRoutedRpc.instance.Register<float, float>("cc_TimerUpdate", EventSpawn.HandleChallengeDone);
    }
}