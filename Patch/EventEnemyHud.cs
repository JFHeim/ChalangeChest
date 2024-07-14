using HarmonyLib;
using UnityEngine.UI;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.UpdateHuds)), HarmonyWrapSafe]
public class EventEnemyHud
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(EnemyHud __instance)
    {
        Debug("UpdateHuds 0");
        var huds = __instance.m_huds.Where(c =>
        {
            var pos = c.Key.m_nview?.GetZDO()?.GetVec3("ChallengeChestPos", Vector3.zero);
            return c.Key != null && pos != null && pos != Vector3.zero && c.Value.m_gui != null;
        }).Select(kv => kv.Value).ToList();

        Debug($"UpdateHuds 1 hudsCount={huds.Count}");
        foreach (var hud in huds)
        {
            var guiRect = hud.m_gui.GetComponent<RectTransform>();
            var nameRect = hud.m_name.GetComponent<RectTransform>();
            if (!guiRect || !nameRect) continue;
            Debug("UpdateHuds 2");

            var alert = hud.m_gui.transform.Find("Alerted");
            if (alert != null) alert.gameObject.SetActive(false);

            Debug("UpdateHuds 3");
            var star = hud.m_gui.transform.FindChildByName("star");
            if (!star || !alert) continue;
            var newStart = hud.m_gui.transform.FindChildByName("cc_EventMobStar") ??
                           Instantiate(star, hud.m_gui.transform);
            Debug("UpdateHuds 4");
            newStart.name = "cc_EventMobStar";
            newStart.position = new Vector3(star.position.x, alert.position.y, alert.position.z);
            newStart.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            Debug("UpdateHuds 5");
            var icon = RegisterPrefabs.Sprite("cc_IconChest");
            if (!icon) continue;
            Debug("UpdateHuds 6");
            var image = newStart.gameObject.GetOrAddComponent<Image>();
            image.sprite = icon;
            image.color = Color.white;
            Debug("UpdateHuds 7");
            newStart.localScale = new Vector3(2f, 2f, 2f);
            Debug("UpdateHuds 8");
            var star1 = newStart.transform.FindChildByName("star (1)")?.gameObject;
            if (star1) Destroy(star1);
            Debug("UpdateHuds 9");
        }

        Debug("UpdateHuds finished");
    }


    private static readonly bool _debug = false;

    private static void Debug(object s)
    {
        if (!_debug) return;
        ModBase.Debug(s);
    }
}