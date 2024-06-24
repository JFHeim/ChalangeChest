using HarmonyLib;
using SoftReferenceableAssets;
using static ChallengeChest.Managers.LocalizationManager.Location;
using static ChallengeChest.Patch.InvokeEventPeriodically;
using static Utils;

namespace ChallengeChest.Patch;

[HarmonyPatch, HarmonyWrapSafe]
public class InvokeEventPeriodically
{
    internal static readonly HashSet<int> PlayerBasePieces = [];
    private static readonly Dictionary<string, Location> Locations = new();
    private static Dictionary<string, SoftReference<GameObject>> _locationReferences = new();
    public static readonly Dictionary<string, Sprite> BossIcons = new();
    public static readonly List<Vector3> CurrentBossPositions = [];
    private static TextMeshProUGUI _bossTimer = null!;

    
    private static readonly List<BossCharacter> bosses = [];
    
	public static void Init()
	{
		bosses.Add(Utils_.ConvertComponent<BossCharacter, Humanoid>(PrefabManager.RegisterPrefab(bundle, "Crystal_Frost_Reaper")));
		bosses.Add(Utils.ConvertComponent<BossCharacter, Humanoid>(PrefabManager.RegisterPrefab(bundle, "Crystal_Flame_Reaper")));
		bosses.Add(Utils.ConvertComponent<BossCharacter, Humanoid>(PrefabManager.RegisterPrefab(bundle, "Crystal_Soul_Reaper")));

		bossSmashAttack = PrefabManager.RegisterPrefab(bundle, "JC_Boss_AOE_Hit_2").GetComponent<Aoe>();
		fireBossAoe = PrefabManager.RegisterPrefab(bundle, "JC_Boss_Explosion_Flames").GetComponent<Aoe>();
		frostBossAoe = PrefabManager.RegisterPrefab(bundle, "JC_Boss_Explosion_Frost").GetComponent<Aoe>();
		poisonBossAoe = PrefabManager.RegisterPrefab(bundle, "JC_Boss_Explosion_Poison").GetComponent<Aoe>();

		plainsConfigs = new BalanceConfig
		{
			health = bosses[0].m_health,
			aoeFire = fireBossAoe.m_damage.m_fire,
			aoeFrost = frostBossAoe.m_damage.m_frost,
			aoePoison = poisonBossAoe.m_damage.m_poison,
			smashBlunt = bossSmashAttack.m_damage.m_blunt,
		};
		foreach (GameObject attackItem in bosses[0].m_randomSets[0].m_items)
		{
			if (attackItem.name == "JC_Reaper_Punch")
			{
				plainsConfigs.punchBlunt = attackItem.GetComponent<ItemDrop>().m_itemData.m_shared.m_damages.m_blunt;
			}
		}
		mistlandsConfigs = new BalanceConfig
		{
			health = 10000f,
			punchBlunt = 140f,
			smashBlunt = 200f,
			aoeFire = 190f,
			aoeFrost = 140f,
			aoePoison = 400f,
		};
		ashlandsConfigs = new BalanceConfig
		{
			health = 15000f,
			punchBlunt = 175f,
			smashBlunt = 250f,
			aoeFire = 230f,
			aoeFrost = 180f,
			aoePoison = 500f,
		};

		PrefabManager.RegisterPrefab(bundle, "Crystal_Frost_Reaper_Cage").AddComponent<RemoveBossDestructible>();
		PrefabManager.RegisterPrefab(bundle, "Crystal_Flame_Reaper_Cage").AddComponent<RemoveBossDestructible>();
		PrefabManager.RegisterPrefab(bundle, "Crystal_Soul_Reaper_Cage").AddComponent<RemoveBossDestructible>();
		PrefabManager.RegisterPrefab(bundle, "JC_Crystal_Reapers_Event").AddComponent<RemoveBossDestructible>().spawned = 3;

		BossSpawn.bossIcons["Crystal_Frost_Reaper_Cage"] = bundle.LoadAsset<Sprite>("JCBossIconBlue");
		BossSpawn.bossIcons["Crystal_Flame_Reaper_Cage"] = bundle.LoadAsset<Sprite>("JCBossIconRed");
		BossSpawn.bossIcons["Crystal_Soul_Reaper_Cage"] = bundle.LoadAsset<Sprite>("JCBossIconGreen");
		BossSpawn.bossIcons["JC_Crystal_Reapers_Event"] = bundle.LoadAsset<Sprite>("JCBossIconEvent");
        
        ZoneSystem.instance.GlobalKeyAdd();
	}
    

    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    private static class SpawnBossCheck
    {
        [HarmonyPostfix, UsedImplicitly]
        private static void Postfix(ZoneSystem __instance)
        {
            if (_locationReferences.Count == 0)
            {
                _locationReferences = Locations.ToDictionary(kv => kv.Key,
                    kv => PrefabManager.AddLoadedSoftReferenceAsset(kv.Value.gameObject));
            }

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


    public static void BroadcastMinimapUpdate()
    {
        ZoneSystem.instance.SendLocationIcons(ZRoutedRpc.Everybody);
        if (!Minimap.instance) return;
        Minimap.instance.UpdateLocationPins(10);
    }


    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Awake))]
    private static class InsertMinimapIcon
    {
        private static void Postfix(Minimap __instance)
        {
            foreach (var kv in BossIcons)
            {
                __instance.m_locationIcons.Add(new Minimap.LocationSpriteData
                    { m_icon = kv.Value, m_name = Locations[kv.Key].name });
            }

            _bossTimer = Instantiate(__instance.m_largeRoot.transform.Find("KeyHints/keyboard_hints/AddPin/Label"),
                __instance.m_largeRoot.transform).GetComponent<TextMeshProUGUI>();
            _bossTimer.name = "ChallengeChest Timer";
            _bossTimer.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
            _bossTimer.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
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
                        _bossTimer.text = Localization.instance.Localize("$jc_gacha_world_boss_spawn",
                            TimeSpan.FromSeconds(nextBossSpawn).ToString("c"));

                        CurrentBossPositions.Clear();

                        foreach (var pin in __instance.m_pins.Where(p => BossIcons.ContainsValue(p.m_icon)))
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

    public static void UpdateBossTimerVisibility()
    {
        if (!_bossTimer) return;
        _bossTimer.gameObject.SetActive(EventSpawnTimer.Value > 0);
    }

    public static void UpdateTimerPosition()
    {
        if (!Minimap.instance) return;
        var rect = (RectTransform)Minimap.instance.m_largeRoot.transform.Find("IconPanel").transform;
        var anchoredPosition = rect.anchoredPosition;
        _bossTimer.GetComponent<RectTransform>().anchoredPosition = new Vector2(-anchoredPosition.x - 30,
            -anchoredPosition.y - 5 - WorldBossCountdownDisplayOffset.Value);
        _bossTimer.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
        _bossTimer.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
        _bossTimer.GetComponent<RectTransform>().sizeDelta = rect.sizeDelta;
        _bossTimer.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
    }

    private static DateTime _lastBossSpawn = DateTime.MinValue;

    public static void SpawnBoss()
    {
        if (_lastBossSpawn == ZNet.instance.GetTime() || GetRandomSpawnPoint() is not { } pos) return;
        long despawnTime = ZNet.instance.GetTime().AddMinutes(TimeLimit.Value).Ticks / 10000000L;

        string boss;

        if (SpawnChance.Value > 0 && Random.value < SpawnChance.Value / 100f) boss = "CC_Event00";
        else boss = BossIcons.Keys.ToList()[Random.Range(0, BossIcons.Count - 1)];

        ZoneSystem.instance.RegisterLocation(new ZoneSystem.ZoneLocation
        {
            m_iconAlways = true,
            m_prefabName = Locations[boss].name,
            m_prefab = _locationReferences[boss],
        }, pos with { y = despawnTime }, true);

        var zdo = ZDOMan.instance.CreateNewZDO(pos, boss.GetStableHashCode());
        zdo.SetPrefab(boss.GetStableHashCode());
        zdo.Persistent = true;
        zdo.Set("ChallengeChestTime", despawnTime);

        _lastBossSpawn = ZNet.instance.GetTime();

        BroadcastMinimapUpdate();
    }

    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.RPC_LocationIcons))]
    private static class UpdateLocationIcons
    {
        [HarmonyPostfix, UsedImplicitly]
        private static void Postfix()
        {
            if (!Minimap.instance || m_localPlayer?.IsDead() != false) return;
            Minimap.instance.UpdateLocationPins(10);
        }
    }

    private static Vector3? GetRandomSpawnPoint()
    {
        for (var i = 0; i < 10000; ++i)
        {
            retry:
            var randomPoint = Random.insideUnitCircle * SpawnMaxDistance.Value;
            Vector3 point = new(randomPoint.x, 0, randomPoint.y);

            if (point.DistanceXZ(Vector3.zero) < SpawnMinDistance.Value) continue;

            var biome = WorldGenerator.instance.GetBiome(point.x, point.z);
            var biomeHeight = WorldGenerator.instance.GetBiomeHeight(biome, point.x, point.z, out _);
            var forestFactor = Minimap.instance.GetMaskColor(point.x, point.z, biomeHeight, biome).r;

            if (biomeHeight < ZoneSystem.instance.m_waterLevel + 5 || forestFactor > 0.75 ||
                (biome == Heightmap.Biome.AshLands && Random.Range(1, 6) != 1) ||
                (biome == Heightmap.Biome.DeepNorth && Random.Range(1, 7) > 2)) continue;

            var baseValue = 0;
            var sector = ZoneSystem.instance.GetZone(point);

            if (ZoneSystem.instance.m_locationInstances.ContainsKey(sector)) continue;

            for (var j = 0; j < 10; ++j)
            {
                var circle = Random.insideUnitCircle * j;
                if (Mathf.Abs(biomeHeight -
                              WorldGenerator.instance.GetBiomeHeight(biome, point.x + circle.x, point.z + circle.y,
                                  out _)) > 5) goto retry;
            }

            if (WorldGenerator.instance.GetBiomeArea(point) == Heightmap.BiomeArea.Edge) continue;

            List<ZDO> zdos = [];
            for (var y = -1; y <= 1; ++y)
            {
                for (var x = -1; x <= 1; ++x)
                {
                    zdos.Clear();
                    ZDOMan.instance.FindObjects(sector + new Vector2i(x, y), zdos);
                    baseValue += zdos.Count(zdo =>
                        PlayerBasePieces.Contains(zdo.m_prefab) && DistanceXZ(zdo.m_position, point) <
                        SpawnBaseDistance.Value);
                }
            }

            if (baseValue > 1) continue;

            return point with { y = WorldGenerator.instance.GetHeight(point.x, point.z) };
        }

        return null;
    }

    public static void HandleChallengeDone(Vector2i sector)
    {
        if (ZoneSystem.instance.m_locationInstances.TryGetValue(sector, out var location)
            && !Approximately(location.m_position.y, (long)location.m_position.y))
        {
            ZoneSystem.instance.m_locationInstances[sector] = location;
            return;
        }

        ZoneSystem.instance.m_locationInstances.Remove(sector);
        BroadcastMinimapUpdate();
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