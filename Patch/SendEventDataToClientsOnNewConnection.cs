using HarmonyLib;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
[HarmonyWrapSafe]
file static class SendEventDataToClientsOnNewConnection
{
    [UsedImplicitly, HarmonyPostfix]
    private static void Postfix()
    {
        if (!ZNet.instance.IsServer()) return;
        EventSpawn.SendEventDataToClients();
    }
}