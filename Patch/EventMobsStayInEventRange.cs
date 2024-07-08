using HarmonyLib;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.Awake)), HarmonyWrapSafe]
file static class EventMobsStayInEventRange
{
    private const float ChaseRangeOutOfEvent = 20;

    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(MonsterAI __instance)
    {
        var isEvent = __instance.m_nview.GetZDO().GetVec3("ChallengeChestPos", Vector3.zero) != Vector3.zero;
        if (!isEvent) return;

        var eventPos = Minimap.instance.m_pins
            .Where(p => EventData.Icons.Values.Contains(p.m_icon))
            .Select(x => x.m_pos)
            .FirstOrDefault(x => x.DistanceXZ(__instance.transform.position) <= EventData.Range + 1);

        if (eventPos == default || eventPos == Vector3.zero) return;
        __instance.m_maxChaseDistance = EventData.Range + ChaseRangeOutOfEvent;
    }
}