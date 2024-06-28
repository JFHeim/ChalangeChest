using HarmonyLib;
using SoftReferenceableAssets;
using static Utils;

namespace ChallengeChest.Patch;

[HarmonyPatch, HarmonyWrapSafe]
public class EventSpawn
{
    public static readonly HashSet<int> PlayerBasePieces = [];
    public static readonly Dictionary<Difficulty, Location> Locations = new();
    public static Dictionary<Difficulty, SoftReference<GameObject>> locationReferences = new();
    public static TextMeshProUGUI bossTimer = null!;

    private static DateTime _lastBossSpawn = DateTime.MinValue;

    public static void Init()
    {
        Debug("Initializing EventSpawn");

        foreach (var kv in EventData.Events)
            Locations.Add(kv.Key, RegisterPrefabs.Prefab(kv.Key.Prefab()).GetComponent<Location>());

        Debug("Done EventSpawn init");
    }

    public static void BroadcastMinimapUpdate()
    {
        Debug("BroadcastMinimapUpdate");
        ZoneSystem.instance.SendLocationIcons(ZRoutedRpc.Everybody);
        if (!Minimap.instance) return;
        Minimap.instance.UpdateLocationPins(10);
    }

    public static void UpdateBossTimerVisibility()
    {
        Debug("UpdateBossTimerVisibility");
        if (!bossTimer) return;
        bossTimer.gameObject.SetActive(EventSpawnTimer.Value > 0);
    }

    public static void UpdateTimerPosition()
    {
        Debug("UpdateTimerPosition 0");
        if (!Minimap.instance) return;
        var rect = (RectTransform)Minimap.instance.m_largeRoot.transform.Find("IconPanel").transform;
        var anchoredPosition = rect.anchoredPosition;
        var rectTransform = bossTimer.GetComponent<RectTransform>();
        Debug("UpdateTimerPosition 1");
        rectTransform.anchoredPosition = new Vector2(-anchoredPosition.x - 30,
            -anchoredPosition.y - 5 - MapDisplayOffset.Value);
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.sizeDelta = rect.sizeDelta;
        rectTransform.pivot = new Vector2(0, 0);
        Debug("UpdateTimerPosition finish");
    }

    public static void SpawnBoss(Vector3? pos = null)
    {
        Debug("SpawnBoss 0");
        if (_lastBossSpawn == ZNet.instance.GetTime()) return;
        pos ??= GetRandomSpawnPoint();
        if (pos is null) return;
        var despawnTime = ZNet.instance.GetTime().AddMinutes(TimeLimit.Value).Ticks / 10000000L;

        var difficulty = EventData.Events.Keys.ToList()[Random.Range(0, Min(0, EventData.Events.Count - 1))];

        ZoneSystem.instance.RegisterLocation(new ZoneSystem.ZoneLocation
        {
            m_iconAlways = true,
            m_prefabName = Locations[difficulty].name,
            m_prefab = locationReferences[difficulty],
            m_name = Locations[difficulty].name
        }, pos.Value with { y = despawnTime }, true);

        Debug("SpawnBoss 1");
        var locationComponent = locationReferences[difficulty].Asset?.GetComponent<Location>();
        if (locationComponent is null)
        {
            DebugError($"Could not find location component for {difficulty}. Aborting");
            return;
        }

        SpawnEventLocation(pos.Value, difficulty, despawnTime, locationComponent.GetMaxRadius());
        _lastBossSpawn = ZNet.instance.GetTime();
        BroadcastMinimapUpdate();
        Debug("SpawnBoss finish");
    }

    private static Vector3? GetRandomSpawnPoint()
    {
        Debug("GetRandomSpawnPoint 0");
        for (var i = 0; i < 10000; ++i)
        {
            retry:
            Debug("GetRandomSpawnPoint 1 retry");
            var randomPoint = Random.insideUnitCircle * SpawnMaxDistance.Value;
            Vector3 point = new(randomPoint.x, 0, randomPoint.y);

            if (point.DistanceXZ(Vector3.zero) < SpawnMinDistance.Value) continue;

            Debug("GetRandomSpawnPoint 2");
            var biome = WorldGenerator.instance.GetBiome(point.x, point.z);
            var biomeHeight = WorldGenerator.instance.GetBiomeHeight(biome, point.x, point.z, out _);
            var forestFactor = Minimap.instance.GetMaskColor(point.x, point.z, biomeHeight, biome).r;

            Debug("GetRandomSpawnPoint 3");
            if (biomeHeight < ZoneSystem.instance.m_waterLevel + 5 || forestFactor > 0.75 ||
                (biome == Heightmap.Biome.AshLands && Random.Range(1, 6) != 1) ||
                (biome == Heightmap.Biome.DeepNorth && Random.Range(1, 7) > 2)) continue;

            var baseValue = 0;
            var sector = ZoneSystem.instance.GetZone(point);

            if (ZoneSystem.instance.m_locationInstances.ContainsKey(sector)) continue;

            Debug("GetRandomSpawnPoint 4");
            for (var j = 0; j < 10; ++j)
            {
                var circle = Random.insideUnitCircle * j;
                if (Mathf.Abs(biomeHeight -
                              WorldGenerator.instance.GetBiomeHeight(biome, point.x + circle.x, point.z + circle.y,
                                  out _)) > 5) goto retry;
            }

            Debug("GetRandomSpawnPoint 5");
            if (WorldGenerator.instance.GetBiomeArea(point) == Heightmap.BiomeArea.Edge) continue;

            List<ZDO> zdos = [];
            for (var y = -1; y <= 1; ++y)
            {
                for (var x = -1; x <= 1; ++x)
                {
                    Debug($"GetRandomSpawnPoint 6, {x}:{y}");
                    zdos.Clear();
                    ZDOMan.instance.FindObjects(sector + new Vector2i(x, y), zdos);
                    baseValue += zdos.Count(zdo =>
                        PlayerBasePieces.Contains(zdo.m_prefab) && zdo.m_position.DistanceXZ(point) <
                        SpawnBaseDistance.Value);
                }
            }

            if (baseValue > 1) continue;

            Debug($"GetRandomSpawnPoint 7, point={point}");
            return point.RoundCords() with { y = WorldGenerator.instance.GetHeight(point.x, point.z) };
        }

        Debug("GetRandomSpawnPoint finish");
        return null;
    }

    public static async void HandleChallengeDone(Vector2i sector)
    {
        if (!ZoneSystem.instance || ZoneSystem.instance.m_locationInstances is null) return;
        if (ZoneSystem.instance.m_locationInstances.TryGetValue(sector, out var location)
            && !Approximately(location.m_position.y, (long)location.m_position.y))
        {
            ZoneSystem.instance.m_locationInstances[sector] = location;
            return;
        }

        ZoneSystem.instance.m_locationInstances.Remove(sector);
        ZoneSystem.instance.StartCoroutine(CheckDespawnEnumerator());

        var eventPos = new Vector3(location.m_position.x,
            WorldGenerator.instance.GetHeight(location.m_position.x, location.m_position.z), location.m_position.z);

        var zdos = await ZoneSystem.instance.GetWorldObjectsAsync(zdo =>
        {
            var mobEventPos = zdo.GetVec3("ChallengeChestPos", Vector3.zero).ToV2();
            return mobEventPos != Vector2.zero && mobEventPos == eventPos.RoundCords().ToV2();
        });

        //Should never happen, if happens it will destroy entire world
        if (zdos.Count > 100)
        {
            DebugError("Too many zdos, looks suspicious... aborting", true, true);
            return;
        }

        ZDO vfx;
        Debug($"Finishing ChallengeChest, {zdos.Count} zdos will be destroyed");
        foreach (var zdo in zdos)
        {
            vfx = ZDOMan.instance.CreateNewZDO(zdo.GetPosition(), VFXHash);
            vfx.SetPrefab(VFXHash);
            ZDOMan.instance.DestroyZDO(zdo);
        }

        var chest = ZDOMan.instance.CreateNewZDO(eventPos, "cc_SuccessChest_normal".GetStableHashCode());
        chest.SetPrefab("cc_SuccessChest_normal".GetStableHashCode());
        chest.SetPosition(eventPos);
        if (location.m_location == null) return;
        Debug("HandleChallengeDone 2 populating chest");
        PopulateChest(chest, location.m_location.m_prefabName.GetDifficultyFromPrefab().Value);
        Debug("HandleChallengeDone 3 spawning vfx");
        vfx = ZDOMan.instance.CreateNewZDO(chest.GetPosition(), VFXHash);
        vfx.SetPrefab(VFXHash);
        SnapToGround.SnappAll();
        BroadcastMinimapUpdate();
        Debug("HandleChallengeDone finish");
    }

    public static IEnumerator CheckDespawnEnumerator()
    {
        var zdosLoaded = false;
        List<ZDO> zdos = null;

        var locationsToRemove = ZoneSystem.instance.m_locationInstances
            .Where(p => p.Value.m_location.m_prefabName.StartsWith("cc_Event_"))
            .Select(x => x.Value.m_position.RoundCords())
            .Where(p => p.y < 1 + (int)ZNet.instance.GetTimeSeconds())
            .Select(x => x.ToV2())
            .ToList();

        if (locationsToRemove.Count > 0)
        {
            foreach (var locationPos in locationsToRemove)
            {
                var zone = locationPos.ToV3().GetZone();
                ZoneSystem.instance.m_locationInstances.Remove(zone);
                LoadLocationZdos(locationPos);
                yield return new WaitUntil(() => zdosLoaded);

                Debug($"CheckDespawnEnumerator Found {zdos.Count} ZDOs in location {locationPos} area");
                var currentTime = ZNet.instance.GetTimeSeconds() + 5;
                zdos = zdos.Where(x => x.GetLong("ChallengeChestTime", long.MaxValue) < currentTime)
                    .ToList();
                foreach (var zdo in zdos)
                {
                    zdo.SetOwner(ZDOMan.instance.m_sessionID);
                    ZDOMan.instance.DestroyZDO(zdo);
                    var vfx = ZDOMan.instance.CreateNewZDO(zdo.GetPosition(), VFXHash);
                    vfx.SetPrefab(VFXHash);
                }
            }

            BroadcastMinimapUpdate();
        }

        yield break;

        async void LoadLocationZdos(Vector2 locationPos)
        {
            zdosLoaded = false;

            zdos = await ZoneSystem.instance.GetWorldObjectsAsync(zdo =>
                zdo.GetVec3("ChallengeChestPos", Vector3.zero).ToV2() == locationPos);

            zdosLoaded = true;
        }
    }

    private static void PopulateChest(ZDO zdo, Difficulty difficulty)
    {
        var prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab())?.GetComponent<Container>();
        if (!prefab)
        {
            DebugError("Could not find prefab " + zdo.GetPrefab());
            return;
        }

        zdo.Set(ZDOVars.s_addedDefaultItems, true);
        Inventory inventory;
        GenerateItems();
        var pkg = new ZPackage();
        inventory.Save(pkg);
        var base64 = pkg.GetBase64();
        zdo.Set(ZDOVars.s_items, base64);
        return;

        void GenerateItems()
        {
            inventory = new Inventory(prefab.m_name, prefab.m_bkg, prefab.m_width, prefab.m_height);
            foreach (var drop in GetChestDrops(WorldGenerator.instance.GetBiome(zdo.GetPosition()), difficulty))
            {
                if (Random.value > drop.ChanceToDropAny) continue;
                var itemPrefab = ObjectDB.instance.GetItemPrefab(drop.PrefabName);
                if (!itemPrefab)
                {
                    DebugWarning($"Failed to find item '{drop.PrefabName}'");
                    continue;
                }

                if (drop.AmountMin < 1)
                {
                    DebugWarning($"Invalid min amount '{drop.AmountMin}' for '{drop.PrefabName}'");
                    continue;
                }

                if (drop.AmountMax < 1)
                {
                    DebugWarning($"Invalid max amount '{drop.AmountMax}' for '{drop.PrefabName}'");
                    continue;
                }

                inventory.AddItem(itemPrefab, Random.Range(drop.AmountMin, drop.AmountMax + 1));
            }
        }
    }

    public static void SpawnEventLocation(Vector3 pos, Difficulty difficulty, long despawnTime, float range)
    {
        var prefab = Locations[difficulty].gameObject;
        var rot = Quaternion.identity;

        var zObjs = GetEnabledComponentsInChildren<ZNetView>(prefab).ToList();
        var randomSpawns = GetEnabledComponentsInChildren<RandomSpawn>(prefab).ToList();

        foreach (var randomSpawn in randomSpawns) randomSpawn.Prepare();

        foreach (var randomSpawn in randomSpawns)
        {
            var position2 = randomSpawn.gameObject.transform.position;
            randomSpawn.Randomize(pos + rot * position2);
        }

        foreach (var netView in zObjs)
        {
            if (!netView.gameObject.activeSelf) continue;
            var selfPosition = netView.gameObject.transform.position;
            var worldPosition = pos + rot * selfPosition;
            var selfRotation = netView.gameObject.transform.rotation;
            var worldRotation = rot * selfRotation;
            // var instance = Instantiate(netView.gameObject, position4, rotation3);

            var hash = netView.GetPrefabName().GetStableHashCode();
            var instance = ZDOMan.instance.CreateNewZDO(worldPosition, hash);
            instance.SetPrefab(hash);
            instance.SetPosition(worldPosition);
            instance.SetRotation(worldRotation);
            instance.Set("ChallengeChestTime", despawnTime);
            instance.Set("ChallengeChestPos", pos.RoundCords() with { y = 0 });
            instance.Persistent = true;

            DebugWarning($"Spawned {instance}");
        }

        foreach (var randomSpawn in randomSpawns) randomSpawn.Reset();
        foreach (var netView in zObjs) netView.gameObject.SetActive(true);

        foreach (var mob in GetEventMobs(WorldGenerator.instance.GetBiome(pos), difficulty))
        {
            var random = Random.value > mob.ChanceToSpawnAny;
            var hash = mob.PrefabName.GetStableHashCode();
            var count = Random.Range(mob.AmountMin, mob.AmountMax + 1);
            Debug(!random
                ? $"Trying to spawn {count} {mob.PrefabName}"
                : $"Skipping {mob.PrefabName} because it's random"); 
            if (random) continue;
            for (var i = 0; i < count; i++)
            {
                var worldPosition = pos + Random.insideUnitSphere * range;
                worldPosition.y = WorldGenerator.instance.GetHeight(pos.x, pos.z);
                var instance = ZDOMan.instance.CreateNewZDO(worldPosition, hash);
                instance.SetPrefab(hash);
                instance.SetPosition(worldPosition);
                instance.Set("ChallengeChestTime", despawnTime);
                instance.Set("ChallengeChestPos", pos.RoundCords() with { y = 0 });
                instance.Set(ZDOVars.s_level, Random.Range(mob.LevelMin, mob.LevelMax + 1));
                instance.Persistent = true;

                //Boost

                DebugWarning($"Spawned {instance} ({mob.PrefabName})");
            }
        }

        SnapToGround.SnappAll();
    }
}