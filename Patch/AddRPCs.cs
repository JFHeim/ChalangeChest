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
    }
}