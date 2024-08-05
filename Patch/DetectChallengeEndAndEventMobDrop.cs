using HarmonyLib;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(Character), nameof(Character.OnDeath)), HarmonyWrapSafe]
file static class DetectChallengeEndAndEventMobDrop
{
    [HarmonyPrefix, UsedImplicitly]
    private static void Prefix(Character __instance)
    {
        if (ModEnabled.Value == false) return;
        if (!__instance.m_nview.IsOwner()) return;
        var eventPos = __instance.m_nview.GetZDO().GetVec3("ChallengeChestPos", Vector3.zero).ToV2().ToSimpleVector2();
        if (eventPos is { x: 0, y: 0 }) return;

        if (!EventMobDrop.Value)
        {
            var drop = __instance.GetComponent<CharacterDrop>();
            //TODO: mob drop from config
            drop.m_drops?.Clear();
        }

        Logic(eventPos, __instance);
    }

    private static async void Logic(SimpleVector2 eventPos, Character itself)
    {
        if (ModEnabled.Value == false) return;
        
        // wait for mob zdo to be destroyed
        // random is needed to make mobs be processed in order and not in the same time
        await Task.Delay(1000 + Random.Range(0, 600)); 

        var myEventMobsNearby = Character.GetAllCharacters()
            .Where(x => x != itself)
            .Select(x => x?.m_nview?.GetZDO())
            .Where(x => x is not null)
            .Select(x => (x, x.GetVec3("ChallengeChestPos", Vector3.zero).ToV2().ToSimpleVector2()))
            .Where(x => x.Item2.Equals(eventPos))
            .Select(x => x.x).ToList();
        if (myEventMobsNearby is { Count: > 0 }) return;

        Debug("DetectChallengeEnd no local mobs, checking globally in world...");
        myEventMobsNearby = await ZoneSystem.instance.GetWorldObjectsAsync(zdo =>
            zdo.GetVec3("ChallengeChestPos", Vector3.zero).ToV2().ToSimpleVector2().Equals(eventPos));
        if (myEventMobsNearby is { Count: > 0 }) return;

        Debug($"DetectChallengeEnd Calling HandleChallengeDone on pos={eventPos}");
        ZRoutedRpc.instance.InvokeRoutedRPC("cc_HandleChallengeDone", (double)eventPos.x, (double)eventPos.y);
    }
}