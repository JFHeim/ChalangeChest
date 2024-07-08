using HarmonyLib;
using static ChallengeChest.EventSpawn;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(Minimap), nameof(Minimap.Awake))]
[HarmonyWrapSafe]
file static class MinimapLabel
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(Minimap __instance)
    {
        foreach (var pair in EventData.Icons)
        {
            __instance.m_locationIcons.Add(new Minimap.LocationSpriteData
                { m_icon = pair.Value, m_name = pair.Value.name });
        }

        bossTimer = Instantiate(__instance.m_largeRoot.transform.Find("KeyHints/keyboard_hints/AddPin/Label"),
            __instance.m_largeRoot.transform).GetComponent<TextMeshProUGUI>();
        bossTimer.name = "ChallengeChest Timer";
        bossTimer.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
        bossTimer.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
        UpdateTimerPosition();
        UpdateBossTimerVisibility();

        __instance.StartCoroutine(Check());
        return;

        IEnumerator Check()
        {
            while (Minimap.instance)
            {
                yield return new WaitForSeconds(2);

                if (EventSpawnTimer.Value <= 0) yield return new WaitForSeconds(1);
                else
                {
                    var nextBossSpawn = EventSpawnTimer.Value * 60 -
                                        (int)ZNet.instance.GetTimeSeconds() % (EventSpawnTimer.Value * 60) - 1;
                    var timeSpan = TimeSpan.FromSeconds(nextBossSpawn);
                    bossTimer.text = Localization.instance.Localize("$cc_next_event_spawn",
                        timeSpan.ToHumanReadableString());

                    foreach (var pin in __instance.m_pins
                                 .Where(p => EventData.Icons.Values.Contains(p.m_icon)))
                    {
                        pin.m_name = TimeSpan.FromSeconds((int)pin.m_pos.y - (int)ZNet.instance.GetTimeSeconds())
                            .ToString("c");
                        if (pin.m_NamePinData is null)
                        {
                            pin.m_NamePinData = new Minimap.PinNameData(pin);
                            if (__instance.IsPointVisible(pin.m_pos, __instance.m_mapImageLarge))
                                __instance.CreateMapNamePin(pin, __instance.m_pinNameRootLarge);
                        }

                        if (pin.m_NamePinData.PinNameGameObject)
                            pin.m_NamePinData.PinNameText.text = pin.m_name;
                    }
                }
            }
        }
    }
}

[HarmonyWrapSafe]
file static class ShowMinimapIcon
{
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GetLocationIcons))]
    [HarmonyPostfix, UsedImplicitly]
    private static void GetLocationIconsPostfix(Minimap __instance, Dictionary<Vector3, string> icons)
    {
        for (var i = 0; i < icons.Count; i++)
        {
            var icon = icons.ElementAt(i);
            if (icon.Value != EventData.PrefabName) continue;
            var location = ZoneSystem.instance.m_locationInstances.Values.ToList().Find(x => x.m_position == icon.Key);
            if (location.m_location == null || location.m_position == default) continue;
            icons[icon.Key] = location.m_location.m_prefabName;
        }
    }
}