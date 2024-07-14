using HarmonyLib;
using static Utils;

namespace ChallengeChest;

[HarmonyPatch, HarmonyWrapSafe]
public class EventSpawn
{
    public static readonly HashSet<int> PlayerBasePieces = [];
    public static TextMeshProUGUI bossTimer = null!;
    public static List<EventData> EventDatas = [];

    private static DateTime _lastBossSpawn = DateTime.MinValue;

    public static void Init()
    {
        Debug("Initializing EventSpawn");
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
        if (!Minimap.instance) return;
        var rect = (RectTransform)Minimap.instance.m_largeRoot.transform.Find("IconPanel").transform;
        var anchoredPosition = rect.anchoredPosition;
        var rectTransform = bossTimer.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(-anchoredPosition.x - 30,
            -anchoredPosition.y - 5 - MapDisplayOffset.Value);
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.sizeDelta = rect.sizeDelta;
        rectTransform.pivot = new Vector2(0, 0);
    }

    public static void SpawnBoss(Vector3? pos = null)
    {
        Debug("SpawnBoss 0");
        if (_lastBossSpawn == ZNet.instance.GetTime()) return;
        Debug("SpawnBoss 1");
        if (pos == null) pos = GetRandomSpawnPoint();
        else pos = pos.Value with { y = WorldGenerator.instance.GetHeight(pos.Value.x, pos.Value.z) };

        Debug("SpawnBoss 2");
        if (pos is null) return;
        Debug("SpawnBoss 3");
        var despawnTime = ZNet.instance.GetTime().AddMinutes(TimeLimit.Value).Ticks / 10000000L;
        Debug("SpawnBoss 4");

        Debug("SpawnBoss 5");
        var difficulty = Enum.GetValues(typeof(Difficulty)).Cast<Difficulty>().ToList().Random();
        Debug("SpawnBoss 6");

        AddLocationPin();

        Debug("SpawnBoss 7");
        var locationComponent = EventSetup.locationReference.Asset?.GetComponent<Location>();
        if (locationComponent is null)
        {
            DebugError($"Could not find location component for {difficulty}. Aborting");
            return;
        }

        SpawnEventLocation(pos.Value, difficulty, despawnTime, locationComponent.GetMaxRadius());
        _lastBossSpawn = ZNet.instance.GetTime();
        BroadcastMinimapUpdate();
        Debug("SpawnBoss finish");
        return;

        void AddLocationPin()
        {
            var zoneLocation = new ZoneSystem.ZoneLocation
            {
                m_iconAlways = true,
                m_prefabName = EventSetup.Icons[WorldGenerator.instance.GetBiome(pos.Value)].name,
                m_prefab = EventSetup.locationReference,
            };
            var locationRegPos = pos.Value with { y = despawnTime };

            /* We can't use method ZoneSystem.instance.RegisterLocation
             * because some location can already exist in that very zone,
             * so we need to add it manually on some random zone where real location
             * doesn't exist and can not possibly exist.
             */
            Vector2i? zone = null;
            while (zone is null || ZoneSystem.instance.m_locationInstances.ContainsKey(zone.Value))
                zone = new Vector2i(Random.Range(10000, 100000), Random.Range(10000, 100000));

            AddEventData(difficulty,
                pos.Value.ToV2().ToSimpleVector2(),
                zone.Value.ToVector2().ToSimpleVector2(),
                despawnTime);

            var locationInstance = new ZoneSystem.LocationInstance
            {
                m_location = zoneLocation,
                m_position = locationRegPos,
                m_placed = true
            };

            ZoneSystem.instance.m_locationInstances.Add(zone.Value, locationInstance);

            Debug($"SpawnBoss 07 AddLocationPin" +
                  $" locationInstance={locationInstance} at pos={pos.Value.ToSimpleVector3()} zone={zone}");
        }
    }

    private static void AddEventData(Difficulty difficulty, SimpleVector2 pos, SimpleVector2 zone, long despawnTime)
    {
        EventDatas.Add(new EventData(difficulty, pos, zone, despawnTime));
    }

    private static Vector3? GetRandomSpawnPoint()
    {
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

    public static async void HandleChallengeDone(Vector2 eventPos)
    {
        if (!ZoneSystem.instance || ZoneSystem.instance.m_locationInstances is null) return;
        Debug($"HandleChallengeDone 0 eventPos={eventPos}");
        eventPos = eventPos.RoundCords();
        Debug($"HandleChallengeDone 1 eventPos={eventPos} zone={eventPos.ToV3().GetZone()}");

        var data = EventDatas.Find(x => x.pos.Distance(eventPos) < 40);

        if (data is null)
        {
            DebugError($"HandleChallengeDone Could not find location for {eventPos}");
            return;
        }

        Debug($"HandleChallengeDone 2 data={data?.ToString() ?? "null"}");
        // var vector2I = ZoneSystem.instance.m_locationInstances.Keys.ToList().Find(x => x.Equals(data.GetZone()));
        ZoneSystem.instance.m_locationInstances.Remove(data.GetZone());
        EventDatas.Remove(data);
        BroadcastMinimapUpdate();
        ZoneSystem.instance.StartCoroutine(CheckDespawnEnumerator());

        // var eventPos = new Vector3(location.m_position.x,
        //     WorldGenerator.instance.GetHeight(location.m_position.x, location.m_position.z), location.m_position.z);

        Debug("HandleChallengeDone 7");
        if (!ZNet.instance.IsServer()) return;
        var zdos = (await ZoneSystem.instance.GetWorldObjectsAsync(zdo =>
            {
                var mobEventPos = zdo.GetVec3("ChallengeChestPos", Vector3.zero).ToV2();
                return mobEventPos != Vector2.zero;
            })).Select(zdo =>
                (zdo, eventPos: zdo.GetVec3("ChallengeChestPos", Vector3.zero).ToV2().ToSimpleVector2()))
            .ToList();
        Debug($"HandleChallengeDone 8 zdos={zdos.GetString()}");
        zdos = zdos.Where(x => x.eventPos.Equals(data.pos, 4)).ToList();
        Debug($"HandleChallengeDone 9 zdos={zdos.GetString()}");

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
            vfx = ZDOMan.instance.CreateNewZDO(zdo.zdo.GetPosition(), VFXHash);
            vfx.SetPrefab(VFXHash);
            ZDOMan.instance.DestroyZDO(zdo.zdo);
        }

        var chest = ZDOMan.instance.CreateNewZDO(eventPos, "cc_SuccessChest_normal".GetStableHashCode());
        chest.SetPrefab("cc_SuccessChest_normal".GetStableHashCode());
        var chestPos = eventPos.ToV3() with { y = WorldGenerator.instance.GetHeight(eventPos.x, eventPos.y) };
        chest.SetPosition(chestPos);
        Debug("HandleChallengeDone 2 populating chest");
        PopulateChest(chest, data);
        Debug("HandleChallengeDone 3 spawning vfx");
        vfx = ZDOMan.instance.CreateNewZDO(chest.GetPosition(), VFXHash);
        vfx.SetPrefab(VFXHash);
        SnapToGround.SnappAll();
        Debug("HandleChallengeDone finish");
    }

    public static IEnumerator CheckDespawnEnumerator()
    {
        var zdosLoaded = false;
        List<ZDO> zdos = null;

        var allLocations = ZoneSystem.instance.m_locationInstances
            .Values
            .Where(p => EventSetup.Icons.Values.Any(x => x.name == p.m_location.m_prefabName))
            .Select(x => (pos: x.m_position.RoundCords(), location: x.m_location))
            .ToList();

        if (allLocations.Count <= 0) yield break;

        var locationsToRemove = allLocations
            .Where(p => p.pos.y < 1 + (int)ZNet.instance.GetTimeSeconds())
            .Select(pair => pair.pos)
            .Select(pos => (pos, data: EventDatas.Find(d => d.pos.Equals(pos.ToV2(), 4))))
            .ToList();


        Debug($"CheckDespawnEnumerator, Found a " +
              $"total of !{allLocations.Count}! locations and " +
              $"!{locationsToRemove.Count}! locations to remove");
        if (locationsToRemove.Count <= 0) yield break;

        foreach (var info in locationsToRemove)
        {
            Debug($"Removing location {info.pos.ToSimpleVector3()} with data={info.data?.ToString() ?? "null"}");
            if (info.data is null)
            {
                var str = EventDatas.Select(x => $"\n{x}").GetString();
                DebugWarning(
                    $"EventDatas={(str.IsGood() ? str : "empty")}\n" +
                    $"info.pos={info.pos}");
            }

            var zone = info.data?.GetZone() ?? info.pos.GetZone();
            ZoneSystem.instance.m_locationInstances.Remove(zone);
            if (info.data is not null) EventDatas.Remove(info.data);
            if (!ZNet.instance.IsServer()) continue;
            if (info.data is null) continue;
            LoadLocationZdos(info.data);
            yield return new WaitUntil(() => zdosLoaded);

            Debug($"CheckDespawnEnumerator Found {zdos.Count} ZDOs in location {info} area");
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
        yield break;

        async void LoadLocationZdos(EventData data)
        {
            zdosLoaded = false;

            zdos = await ZoneSystem.instance.GetWorldObjectsAsync(zdo =>
                data.pos.Equals(zdo.GetVec3("ChallengeChestPos", Vector3.zero).ToV2()));

            zdosLoaded = true;
        }
    }

    private static void PopulateChest(ZDO zdo, EventData data)
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
            foreach (var drop in GetChestDrops(WorldGenerator.instance.GetBiome(zdo.GetPosition()), data.difficulty))
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
        var prefab = EventSetup.Prefab;
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