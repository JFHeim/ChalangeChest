using HarmonyLib;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.Awake)), HarmonyWrapSafe]
file static class EventMobsStayInEventRange
{
    private static readonly float ChaseRangeOutOfEvent = 20;

    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(MonsterAI __instance)
    {
        var isEvent = __instance.m_nview.GetZDO().GetVec3("ChallengeChestPos", Vector3.zero) != Vector3.zero;
        if (!isEvent) return;

        var @event = Minimap.instance.m_pins
            .Where(p => EventData.Events.Select(x => x.Value.Icon).ToList().Contains(p.m_icon))
            .Select(x => (
                pos: x.m_pos,
                data: EventData.Events.Values.ToList().Find(eventData => eventData.Icon.name == x.m_icon.name)))
            .Where(x => x.pos.DistanceXZ(__instance.transform.position) <= x.data.Range + 1)
            .Select(x => x.data)
            .First();

        __instance.m_maxChaseDistance = @event.Range + ChaseRangeOutOfEvent;
    }
}