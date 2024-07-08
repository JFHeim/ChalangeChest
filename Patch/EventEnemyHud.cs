using HarmonyLib;
using UnityEngine.UI;

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
            // var health = hud.m_gui.transform.Find("Health");
            // if (health) { }

            var alert = hud.m_gui.transform.Find("Alerted");
            if (alert != null) alert.gameObject.SetActive(false);

            var star = hud.m_gui.transform.FindChildByName("star");
            if (!star || !alert) continue;
            var newStart = hud.m_gui.transform.FindChildByName("cc_EventMobStar") ??
                           Instantiate(star, hud.m_gui.transform);
            newStart.name = "cc_EventMobStar";
            newStart.position = new Vector3(star.position.x, alert.position.y, alert.position.z);
            newStart.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            var icon = RegisterPrefabs.Sprite("cc_EventMobIcon");
            if (!icon) continue;
            Destroy(newStart.GetComponent<Sprite>());
            var image = newStart.transform.FindChildByName("star (1)").gameObject.GetOrAddComponent<Image>();
            image.sprite = icon;
        }
    }
}