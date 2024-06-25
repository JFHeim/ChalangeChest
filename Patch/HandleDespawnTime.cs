// using HarmonyLib;
//
// namespace ChallengeChest.Patch;
//
// [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.Awake)), HarmonyWrapSafe]
// public static class HandleDespawnTime
// {
//     [HarmonyPostfix, UsedImplicitly]
//     private static void Postfix(ZNetView __instance)
//     {
//         if(__instance.GetZDO().GetLong("ChallengeChestTime", -1L) <= 0) return;
//         
//         
//     }
// }