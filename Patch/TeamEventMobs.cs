using HarmonyLib;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsEnemy), [typeof(Character), typeof(Character)]), HarmonyWrapSafe]
file static class TeamEventMobs
{
    [HarmonyPrefix, UsedImplicitly]
    private static bool Prefix(Character a, Character b, ref bool __result)
    {
        if (ModEnabled.Value == false) return true;
        if (a.GetFaction() == b.GetFaction()) return true;
        var aIsEvent = a.m_nview.GetZDO().GetVec3("ChallengeChestPos", Vector3.zero) != Vector3.zero;
        var bIsEvent = b.m_nview.GetZDO().GetVec3("ChallengeChestPos", Vector3.zero) != Vector3.zero;
        if (!aIsEvent || !bIsEvent) return true;

        __result = false;
        return false;
    }
}