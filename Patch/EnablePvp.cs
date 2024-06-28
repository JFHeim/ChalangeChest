using HarmonyLib;
using UnityEngine.SceneManagement;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(Player), nameof(Player.Update)), HarmonyWrapSafe]
file static class EnablePvp
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(Player __instance)
    {
        var pl = m_localPlayer;
        if (__instance != pl) return;
        if (!Minimap.instance) return;
        if (!ForcePvp.Value) return;
        if (SceneManager.GetActiveScene().name != "main") return;

        if (pl.IsPVPEnabled()) return;
        var pins = Minimap.instance.m_pins
            .Where(p => EventData.Events.Select(x => x.Value.Icon).ToList().Contains(p.m_icon))
            .Select(x=> (x.m_pos, EventData.Events.Values.ToList().Find(eventData => eventData.Icon.name == x.m_icon.name)))
            .ToList();

        foreach ((Vector3 pos, EventData data) pair in pins)
        {
            var distance = pl.transform.DistanceXZ(pair.pos);
            if (distance > pair.data.Range + 20) continue;
            pl.SetPVP(true);
            return;
        }
    }
}