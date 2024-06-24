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
    public static readonly Dictionary<Difficulty, Sprite> Icons = new();
    public static readonly List<Vector3> CurrentBossPositions = [];
    public static TextMeshProUGUI bossTimer = null!;

    private static DateTime _lastBossSpawn = DateTime.MinValue;

    public static void Init()
    {
        Debug($"Initializing EventSpawn");

        foreach (var kv in Icons)
            Locations.Add(kv.Key, RegisterPrefabs.Prefab(kv.Key.Prefab()).GetComponent<Location>());

        Debug($"Done EventSpawn init");
    }

    public static void BroadcastMinimapUpdate()
    {
        Debug($"BroadcastMinimapUpdate");
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
        Debug("UpdateTimerPosition 1");
        var anchoredPosition = rect.anchoredPosition;
        Debug("UpdateTimerPosition 2");
        var rectTransform = bossTimer.GetComponent<RectTransform>();
        Debug("UpdateTimerPosition 3");
        rectTransform.anchoredPosition = new Vector2(-anchoredPosition.x - 30,
            -anchoredPosition.y - 5 - WorldBossCountdownDisplayOffset.Value);
        Debug("UpdateTimerPosition 4");
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.sizeDelta = rect.sizeDelta;
        rectTransform.pivot = new Vector2(0, 0);
        Debug("UpdateTimerPosition finish");
    }

    public static void SpawnBoss()
    {
        Debug("SpawnBoss 0");
        if (_lastBossSpawn == ZNet.instance.GetTime() || GetRandomSpawnPoint() is not { } pos) return;
        Debug("SpawnBoss 1");
        var despawnTime = ZNet.instance.GetTime().AddMinutes(TimeLimit.Value).Ticks / 10000000L;
        Debug("SpawnBoss 2");

        var difficulty = Icons.Keys.ToList()[Random.Range(0, Icons.Count - 1)];

        Debug("SpawnBoss 3");
        ZoneSystem.instance.RegisterLocation(new ZoneSystem.ZoneLocation
        {
            m_iconAlways = true,
            m_prefabName = Locations[difficulty].name,
            m_prefab = locationReferences[difficulty],
        }, pos with { y = despawnTime }, true);

        Debug("SpawnBoss 4");
        SpawnEventLocation(pos, difficulty, despawnTime);
        Debug("SpawnBoss 5");
        _lastBossSpawn = ZNet.instance.GetTime();
        Debug("SpawnBoss 6");
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
            return point with { y = WorldGenerator.instance.GetHeight(point.x, point.z) };
        }

        Debug($"GetRandomSpawnPoint finish");
        return null;
    }

    public static async void HandleChallengeDone(Vector2i sector)
    {
        Debug($"HandleChallengeDone 0 {sector}");
        if (ZoneSystem.instance.m_locationInstances.TryGetValue(sector, out var location)
            && !Approximately(location.m_position.y, (long)location.m_position.y))
        {
            ZoneSystem.instance.m_locationInstances[sector] = location;
            return;
        }

        ZoneSystem.instance.m_locationInstances.Remove(sector);

        var zdos = await ZoneSystem.instance.GetWorldObjectsAsync(zdo =>
            zdo.GetLong("ChallengeChestTime", -1L) != -1L);
        foreach (var zdo in zdos)
        {
            ZDOMan.instance.CreateNewZDO(zdo.GetPosition(), "vfx_Place_workbench".GetStableHashCode());
            ZDOMan.instance.DestroyZDO(zdo);
        }

        var position = new Vector3(location.m_position.x,
            WorldGenerator.instance.GetHeight(location.m_position.x, location.m_position.z), location.m_position.z);
        var chest = ZDOMan.instance.CreateNewZDO(position, "cc_SuccessChest_normal".GetStableHashCode());
        chest.SetPrefab("cc_SuccessChest_normal".GetStableHashCode());
        chest.SetPosition(position);
        ZDOMan.instance.CreateNewZDO(chest.GetPosition(), "vfx_Place_workbench".GetStableHashCode());
        PopulateChest(chest, location.m_location.m_prefabName.GetDifficultyFromPrefab());
        SnapToGround.SnappAll();

        BroadcastMinimapUpdate();
        Debug($"HandleChallengeDone finish");
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
            switch (difficulty)
            {
                case Difficulty.Normal:
                    foreach (var drop in new SerializedDrops(ItemsNormal.Value).Items)
                    {
                        if (Random.value > drop.ChanceToDropAny) continue;
                        var itemPrefab = ObjectDB.instance.GetItemPrefab(drop.ItemName);
                        if (!itemPrefab)
                        {
                            DebugWarning($"Failed to find item '{drop.ItemName}'");
                            continue;
                        }

                        if (drop.AmountMin < 1)
                        {
                            DebugWarning($"Invalid min amount '{drop.AmountMin}' for '{drop.ItemName}'");
                            continue;
                        }

                        if (drop.AmountMax < 1)
                        {
                            DebugWarning($"Invalid max amount '{drop.AmountMax}' for '{drop.ItemName}'");
                            continue;
                        }

                        inventory.AddItem(itemPrefab, Random.Range(drop.AmountMin, drop.AmountMax + 1));
                    }

                    break;
            }
        }
    }

    public static void SpawnEventLocation(Vector3 pos, Difficulty difficulty, long despawnTime)
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

            DebugWarning($"Spawned {instance}");
        }

        foreach (var randomSpawn in randomSpawns) randomSpawn.Reset();
        foreach (var netView in zObjs) netView.gameObject.SetActive(true);

        SnapToGround.SnappAll();
    }
}