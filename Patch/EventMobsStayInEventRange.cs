using HarmonyLib;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.Awake)), HarmonyWrapSafe]
file static class EventMobsStayInEventRange
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(MonsterAI __instance)
    {
        if (!__instance.m_nview.IsOwner()) return;
        var eventPos = __instance.m_nview.GetZDO().GetVec3("ChallengeChestPos", Vector3.zero);
        var isEvent = eventPos != Vector3.zero;
        if (!isEvent) return;

        if (!(__instance.transform.position.DistanceXZ(eventPos) > EventSetup.Range + 40)) return;
        var pos = (eventPos + Random.insideUnitSphere * 4);
        pos.y = WorldGenerator.instance.GetHeight(__instance.transform.position.x, __instance.transform.position.z);
        __instance.transform.position = pos;
    }
}