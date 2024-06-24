using HarmonyLib;
using static ChallengeChest.Managers.LocalizationManager.Location;
using static ChallengeChest.Patch.EventSpawn;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.RPC_LocationIcons))]
file static class UpdateLocationIcons
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix()
    {
        if (!Minimap.instance || m_localPlayer?.IsDead() != false) return;
        Minimap.instance.UpdateLocationPins(10);
    }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection)), HarmonyWrapSafe]
file static class AddRPCs
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(ZNet __instance, ZNetPeer peer)
    {
        if (!__instance.IsServer()) return;
        peer.m_rpc.Register<int, int>("ChallengeChestDone",
            (_, sectorX, sectorY) => HandleChallengeDone(new Vector2i(sectorX, sectorY)));
    }
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
file static class CachePrefabs
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(ZNetScene __instance)
    {
        foreach (var prefab in __instance.m_prefabs.Where(prefab =>
                     prefab.GetComponent<EffectArea>()?.m_type == EffectArea.Type.PlayerBase))
        {
            PlayerBasePieces.Add(prefab.name.GetStableHashCode());
        }
    }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
file static class SpawnBossCheck
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(ZoneSystem __instance)
    {
        if (locationReferences.Count == 0)
            locationReferences = Locations.ToDictionary(kv => kv.Key,
                kv => PrefabManager.AddLoadedSoftReferenceAsset(kv.Value.gameObject));

        if (!ZNet.instance.IsServer()) return;

        __instance.StartCoroutine(Check());
        return;

        IEnumerator Check()
        {
            while (true)
            {
                if (!ZoneSystem.instance) yield break;

                if (EventSpawnTimer.Value <= 0)
                {
                    yield return new WaitForSeconds(1);
                    continue;
                }

                var oldRemainingTime = int.MaxValue;
                var remainingTime = int.MaxValue - 1;
                while (oldRemainingTime > remainingTime || oldRemainingTime > 50)
                {
                    var locationsToRemove = CurrentBossPositions
                        .Where(p => p.y < 1 + (int)ZNet.instance.GetTimeSeconds())
                        .Select(ZoneSystem.instance.GetZone).ToList();

                    if (locationsToRemove.Count > 0)
                    {
                        foreach (var location in locationsToRemove)
                        {
                            List<ZDO> zdos = [];
                            ZoneSystem.instance.m_locationInstances.Remove(location);
                            ZDOMan.instance.FindObjects(location, zdos);

                            var currentTime = ZNet.instance.GetTimeSeconds() + 5;
                            foreach (var zdo in zdos.Where(zdo =>
                                         zdo.GetLong("ChallengeChestTime", long.MaxValue) < currentTime))
                            {
                                zdo.SetOwner(ZDOMan.instance.m_sessionID);
                                ZDOMan.instance.DestroyZDO(zdo);
                            }
                        }

                        BroadcastMinimapUpdate();
                    }

                    yield return new WaitForSeconds(1);
                    oldRemainingTime = remainingTime;
                    if (EventSpawnTimer.Value > 0)
                    {
                        remainingTime = EventSpawnTimer.Value * 60 - (int)ZNet.instance.GetTimeSeconds() %
                            (EventSpawnTimer.Value * 60);
                    }
                }

                SpawnBoss();
            }
            // ReSharper disable once IteratorNeverReturns
        }
    }
}

[HarmonyPatch(typeof(Minimap), nameof(Minimap.Awake))]
file static class InsertMinimapIcon
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(Minimap __instance)
    {
        foreach (var kv in Icons)
        {
            __instance.m_locationIcons.Add(new Minimap.LocationSpriteData
                { m_icon = kv.Value, m_name = Locations[kv.Key].name });
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
            while (true)
            {
                if (EventSpawnTimer.Value > 0)
                {
                    var nextBossSpawn = EventSpawnTimer.Value * 60 -
                                        (int)ZNet.instance.GetTimeSeconds() %
                                        (EventSpawnTimer.Value * 60) - 1;
                    bossTimer.text = Localization.instance.Localize("$jc_gacha_world_boss_spawn",
                        TimeSpan.FromSeconds(nextBossSpawn).ToString("c"));

                    CurrentBossPositions.Clear();

                    foreach (var pin in __instance.m_pins.Where(p => Icons.ContainsValue(p.m_icon)))
                    {
                        pin.m_name = TimeSpan.FromSeconds((int)pin.m_pos.y - (int)ZNet.instance.GetTimeSeconds())
                            .ToString("c");
                        if (pin.m_NamePinData is null)
                        {
                            pin.m_NamePinData = new Minimap.PinNameData(pin);
                            if (__instance.IsPointVisible(pin.m_pos, __instance.m_mapImageLarge))
                            {
                                __instance.CreateMapNamePin(pin, __instance.m_pinNameRootLarge);
                            }
                        }

                        if (pin.m_NamePinData.PinNameGameObject)
                        {
                            pin.m_NamePinData.PinNameText.text = pin.m_name;
                        }

                        CurrentBossPositions.Add(pin.m_pos);
                    }
                }

                yield return new WaitForSeconds(1);
            }
            // ReSharper disable once IteratorNeverReturns
        }
    }
}