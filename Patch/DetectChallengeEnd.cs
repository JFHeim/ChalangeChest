using HarmonyLib;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(Character), nameof(Character.OnDeath)), HarmonyWrapSafe]
file static class DetectChallengeEnd
{
    [HarmonyPrefix, UsedImplicitly]
    private static void Prefix(Character __instance)
    {
        var eventPos = __instance.m_nview.GetZDO().GetVec3("ChallengeChestPos", Vector3.zero);
        Debug($"eventPos = {eventPos}, zone = {eventPos.GetZone()}");
        if (eventPos == Vector3.zero) return;
        Logic(eventPos, __instance);
    }

    private static async void Logic(Vector3 eventPos, Character itself)
    {
        Character.GetAllCharacters()
            .Where(x => x != itself)
            .Select(x => x?.m_nview?.GetZDO())
            .Where(x => x is not null)
            .Select(x => (x, x.GetVec3("ChallengeChestPos", Vector3.zero)))
            .Where(x => x.Item2 != Vector3.zero)
            .GroupBy(tuple => tuple.Item2)
            .ToDictionary(
                group => group.Key,
                group => group.Select(tup => tup.x).ToList()
            ).TryGetValue(eventPos, out var myEventMobsNearby);
        Debug($"DetectChallengeEnd Local event mobs nearby: {myEventMobsNearby?.Count.ToString() ?? "null"}");
        if (myEventMobsNearby == null || myEventMobsNearby.Count > 0) return;

        myEventMobsNearby = await ZoneSystem.instance.GetWorldObjectsAsync(zdo =>
            zdo.GetVec3("ChallengeChestPos", Vector3.zero) == eventPos);
        Debug($"DetectChallengeEnd World event mobs nearby: {myEventMobsNearby?.Count.ToString() ?? "null"}");
        if (myEventMobsNearby == null || myEventMobsNearby.Count > 0) return;

        var sector = eventPos.GetZone();
        Debug($"DetectChallengeEnd Calling HandleChallengeDone on sector {sector}");
        EventSpawn.HandleChallengeDone(sector);
    }
}