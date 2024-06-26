using HarmonyLib;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(Character), nameof(Character.OnDeath)), HarmonyWrapSafe]
file static class DetectChallengeEnd
{
    [HarmonyPrefix, UsedImplicitly]
    private static void Prefix(Character __instance)
    {
        if (!__instance.m_nview.IsOwner()) return;
        var eventPos = __instance.m_nview.GetZDO().GetVec3("ChallengeChestPos", Vector3.zero).ToV2();
        if (eventPos is { x: 0, y: 0 }) return;

        var drop = __instance.GetComponent<CharacterDrop>();
        //TODO: mob drop from config
        drop.m_drops?.Clear();

        Logic(eventPos, __instance);
    }

    private static async void Logic(Vector2 eventPos, Character itself)
    {
        await Task.Delay(1000); // wait for mob zdo to be destroyed

        var myEventMobsNearby = Character.GetAllCharacters()
            .Where(x => x != itself)
            .Select(x => x?.m_nview?.GetZDO())
            .Where(x => x is not null)
            .Select(x => (x, x.GetVec3("ChallengeChestPos", Vector3.zero).ToV2()))
            .Where(x => x.Item2 == eventPos)
            .Select(x => x.x).ToList();
        if (myEventMobsNearby is { Count: > 0 }) return;

        myEventMobsNearby = await ZoneSystem.instance.GetWorldObjectsAsync(zdo =>
            zdo.GetVec3("ChallengeChestPos", Vector3.zero).ToV2() == eventPos);
        if (myEventMobsNearby is { Count: > 0 }) return;

        var sector = eventPos.ToV3().GetZone();
        Debug($"DetectChallengeEnd Calling HandleChallengeDone on sector {sector}");
        EventSpawn.HandleChallengeDone(sector);
    }
}