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
        if (SceneManager.GetActiveScene().name != "main") return;

        if (pl.IsPVPEnabled()) return;
        var pins = Minimap.instance.m_pins
            .Where(p => EventData.Events.Select(x => x.Value.Icon).ToList().Contains(p.m_icon))
            .ToList();

        foreach (var pin in pins)
        {
            var data = EventData.Events.Values.ToList().Find(x => x.Icon.name == pin.m_icon.name);
            var distance = pl.transform.DistanceXZ(pin.m_pos);
            var flag = distance > data.Range + 20;
            if (flag) continue;
            // pl.m_nview.GetZDO().Set("cc_pvp", pl.IsPVPEnabled());
            pl.SetPVP(true);
            return;
        }
    }
}