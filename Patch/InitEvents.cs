// using HarmonyLib;
//
// namespace ChallengeChest.Patch;
//
// [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
// [HarmonyWrapSafe]
// file static class InitEvents
// {
//     [HarmonyPostfix, UsedImplicitly]
//     private static void Postfix()
//     {
//         EventSetup.Init();
//         EventSpawn.Init();
//     }
// }