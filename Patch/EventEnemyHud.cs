using HarmonyLib;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.UpdateHuds)), HarmonyWrapSafe]
public class EventEnemyHud
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(EnemyHud __instance)
    {
        var huds = __instance.m_huds.Where(c =>
        {
            var pos = c.Key.m_nview?.GetZDO()?.GetVec3("ChallengeChestPos", Vector3.zero);
            return c.Key != null && pos != null && pos != Vector3.zero && c.Value.m_gui != null;
        }).Select(kv => kv.Value).ToList();

        foreach (var hud in huds)
        {
            var guiRect = hud.m_gui.GetComponent<RectTransform>();
            var nameRect = hud.m_name.GetComponent<RectTransform>();
            if (!guiRect || !nameRect) continue;
            var health = hud.m_gui.transform.Find("Health");
            if (health)
            {
            }

            var stars = hud.m_gui.transform.Find("Stars");
            if (stars != null)
            {
            }
        }
    }
}