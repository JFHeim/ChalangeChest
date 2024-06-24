#nullable enable
using System.ComponentModel;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using SoftReferenceableAssets;

// ReSharper disable once CheckNamespace
namespace ChallengeChest.Managers.LocalizationManager;

using Object = Object;

public enum ShowIcon
{
    Always,
    Never,
    Explored,
}

public enum Rotation
{
    Fixed,
    Random,
    Slope,
}

[PublicAPI]
public struct Range(float min, float max)
{
    public float Min = min;
    public float Max = max;
}

[PublicAPI]
public class Location
{
    public bool CanSpawn = true;
    public Heightmap.Biome Biome = Heightmap.Biome.Meadows;

    [Description(
        "If the location should spawn more towards the edge of the biome or towards the center.\nUse 'Edge' to make it spawn towards the edge.\nUse 'Median' to make it spawn towards the center.\nUse 'Everything' if it doesn't matter.")]
    public Heightmap.BiomeArea SpawnArea = Heightmap.BiomeArea.Everything;

    [Description(
        "Maximum number of locations to spawn in.\nDoes not mean that this many locations will spawn. But Valheim will try its best to spawn this many, if there is space.")]
    public int Count = 1;

    [Description(
        "If set to true, this location will be prioritized over other locations, if they would spawn in the same area.")]
    public bool Prioritize;

    [Description(
        "If set to true, Valheim will try to spawn your location as close to the center of the map as possible.")]
    public bool PreferCenter;

    [Description(
        "If set to true, all other locations will be deleted, once the first one has been discovered by a player.")]
    public bool Unique;

    [Description("The name of the group of the location, used by the minimum distance from group setting.")]
    public string GroupName;

    [Description("Locations in the same group will keep at least this much distance between each other.")]
    public float MinimumDistanceFromGroup;

    [Description(
        "When to show the map icon of the location. Requires an icon to be set.\nUse 'Never' to not show a map icon for the location.\nUse 'Always' to always show a map icon for the location.\nUse 'Explored' to start showing a map icon for the location as soon as a player has explored the area.")]
    public ShowIcon ShowMapIcon = ShowIcon.Never;

    [Description("Sets the map icon for the location.")]
    public string? MapIcon
    {
        get => _mapIconName;
        set
        {
            _mapIconName = value;
            MapIconSprite = _mapIconName is null ? null : LoadSprite(_mapIconName);
        }
    }

    private string? _mapIconName;

    [Description("Sets the map icon for the location.")]
    public Sprite? MapIconSprite;

    [Description(
        "How to rotate the location.\nUse 'Fixed' to use the rotation of the prefab.\nUse 'Random' to randomize the rotation.\nUse 'Slope' to rotate the location along a possible slope.")]
    public Rotation Rotation = Rotation.Random;

    [Description("The minimum and maximum height difference of the terrain below the location.")]
    public Range HeightDelta = new(0, 2);

    [Description("If the location should spawn near water.")]
    public bool SnapToWater;

    [Description(
        "If the location should spawn in a forest.\nEverything above 1.15 is considered a forest by Valheim.\n2.19 is considered a thick forest by Valheim.")]
    public Range ForestThreshold = new(0, 2.19f);

    [Description("Minimum and maximum range from the center of the map for the location.")]
    public Range SpawnDistance = new(0, 10000);

    [Description("Minimum and maximum altitude for the location.")]
    public Range SpawnAltitude = new(-1000f, 1000f);

    [Description("If set to true, vegetation is removed inside the location exterior radius.")]
    public bool ClearArea;

    [Description("Adds a creature to a spawner that has been added to the location prefab.")]
    public Dictionary<string, string> CreatureSpawner = new();

    public static bool ConfigurationEnabled = true;

    private readonly global::Location _location;
    private string _folderName = "";
    private AssetBundle? _assetBundle;
    private static readonly List<Location> RegisteredLocations = [];
    private static Dictionary<Location, SoftReference<GameObject>>? _softReferences;

    public Location(string assetBundleFileName, string prefabName, string folderName = "assets") : this(
        PrefabManager.RegisterAssetBundle(assetBundleFileName, folderName), prefabName)
    {
        _folderName = folderName;
    }

    public Location(AssetBundle bundle, string prefabName) : this(bundle.LoadAsset<GameObject>(prefabName))
    {
        _assetBundle = bundle;
    }

    public Location(GameObject location) : this(location.GetComponent<global::Location>())
    {
        if (_location != null) return;
        throw new ArgumentNullException(nameof(location), "The GameObject does not have a location component.");
    }

    public Location(global::Location location)
    {
        _location = location;
        GroupName = location.name;
        RegisteredLocations.Add(this);
    }

    private byte[]? ReadEmbeddedFileBytes(string name)
    {
        using MemoryStream stream = new();
        if (Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name +
                                                                      $"{(_folderName == "" ? "" : ".") + _folderName}." +
                                                                      name) is not { } assemblyStream) return null;

        assemblyStream.CopyTo(stream);
        return stream.ToArray();
    }

    private Texture2D? LoadTexture(string name)
    {
        if (ReadEmbeddedFileBytes(name) is not { } textureData) return null;
        Texture2D texture = new(0, 0);
        texture.LoadImage(textureData);
        return texture;
    }

    private Sprite LoadSprite(string name)
    {
        if (LoadTexture(name) is { } texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, 64, 64), Vector2.zero);
        }

        if (_assetBundle?.LoadAsset<Sprite>(name) is { } sprite)
        {
            return sprite;
        }

        throw new FileNotFoundException($"Could not find a file named {name} for the map icon");
    }

    private static void AddLocationToZoneSystem(ZoneSystem __instance)
    {
        _softReferences ??= RegisteredLocations.ToDictionary(l => l,
            l => PrefabManager.AddLoadedSoftReferenceAsset(l._location.gameObject));

        foreach (var location in RegisteredLocations)
        {
            __instance.m_locations.Add(new ZoneSystem.ZoneLocation
            {
                m_prefabName = location._location.name,
                m_prefab = _softReferences[location],
                m_enable = location.CanSpawn,
                m_biome = location.Biome,
                m_biomeArea = location.SpawnArea,
                m_quantity = location.Count,
                m_prioritized = location.Prioritize,
                m_centerFirst = location.PreferCenter,
                m_unique = location.Unique,
                m_group = location.GroupName,
                m_minDistanceFromSimilar = location.MinimumDistanceFromGroup,
                m_iconAlways = location.ShowMapIcon == ShowIcon.Always,
                m_iconPlaced = location.ShowMapIcon == ShowIcon.Explored,
                m_randomRotation = location.Rotation == Rotation.Random,
                m_slopeRotation = location.Rotation == Rotation.Slope,
                m_snapToWater = location.SnapToWater,
                m_minTerrainDelta = location.HeightDelta.Min,
                m_maxTerrainDelta = location.HeightDelta.Max,
                m_inForest = true,
                m_forestTresholdMin = location.ForestThreshold.Min,
                m_forestTresholdMax = location.ForestThreshold.Max,
                m_minDistance = location.SpawnDistance.Min,
                m_maxDistance = location.SpawnDistance.Max,
                m_minAltitude = location.SpawnAltitude.Min,
                m_maxAltitude = location.SpawnAltitude.Max,
                m_clearArea = location.ClearArea,
                m_exteriorRadius = location._location.m_exteriorRadius,
                m_interiorRadius = location._location.m_interiorRadius,
            });
        }

        DestroyImmediate(__instance.m_locationProxyPrefab.GetComponent<LocationProxy>());
        __instance.m_locationProxyPrefab.AddComponent<LocationProxy>();
    }

    private static void AddLocationZNetViewsToZNetScene(ZNetScene __instance)
    {
        foreach (var netView in RegisteredLocations.SelectMany(l =>
                     l._location.GetComponentsInChildren<ZNetView>(true)))
        {
            if (__instance.m_namedPrefabs.ContainsKey(netView.GetPrefabName().GetStableHashCode()))
            {
                var otherName =
                    Utils_.GetPrefabName(__instance.m_namedPrefabs[netView.GetPrefabName().GetStableHashCode()]);
                if (netView.GetPrefabName() != otherName)
                {
                    DebugError(
                        $"Found hash collision for names of prefabs {netView.GetPrefabName()} and {otherName} in {Assembly.GetExecutingAssembly()}. Skipping.");
                }
            }
            else
            {
                __instance.m_prefabs.Add(netView.gameObject);
                __instance.m_namedPrefabs[netView.GetPrefabName().GetStableHashCode()] = netView.gameObject;
            }
        }

        foreach (var location in RegisteredLocations)
        {
            foreach (var spawner in location._location.transform.GetComponentsInChildren<CreatureSpawner>())
            {
                if (location.CreatureSpawner.TryGetValue(spawner.name, out var creature))
                {
                    spawner.m_creaturePrefab = __instance.GetPrefab(creature);
                }
            }
        }
    }

    private static void AddMinimapIcons(Minimap __instance)
    {
        foreach (var location in RegisteredLocations)
        {
            if (location.MapIconSprite is { } icon)
            {
                __instance.m_locationIcons.Add(new Minimap.LocationSpriteData
                    { m_icon = icon, m_name = location._location.name });
            }
        }
    }

    private class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public int? Order;
    }

    private static bool _firstStartup = true;

    internal static void Patch_FejdStartup()
    {
        if (ConfigurationEnabled && _firstStartup)
        {
            var saveOnConfigSet = Plugin.Config.SaveOnConfigSet;
            Plugin.Config.SaveOnConfigSet = false;

            foreach (var location in RegisteredLocations)
            {
                var order = 0;
                foreach (var kv in location.CreatureSpawner)
                {
                    var spawnerCreature = Config(location._location.name, $"{kv.Key} spawns", kv.Value,
                        new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = --order }));
                    spawnerCreature.SettingChanged += (_, _) =>
                    {
                        location.CreatureSpawner[kv.Key] = spawnerCreature.Value;
                        if (ZNetScene.instance &&
                            location._location.transform.GetComponentsInChildren<CreatureSpawner>()
                                .FirstOrDefault(s => s.name == kv.Key) is { } spawner)
                        {
                            spawner.m_creaturePrefab = ZNetScene.instance.GetPrefab(spawnerCreature.Value);
                        }
                    };
                }
            }

            if (saveOnConfigSet)
            {
                Plugin.Config.SaveOnConfigSet = true;
                Plugin.Config.Save();
            }
        }

        _firstStartup = false;
    }

    static Location()
    {
        Harmony harmony = new("org.bepinex.helpers.LocationManager");
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNetScene), nameof(ZNetScene.Awake)),
            postfix: new HarmonyMethod(
                AccessTools.DeclaredMethod(typeof(Location), nameof(AddLocationZNetViewsToZNetScene)),
                Priority.VeryLow));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations)),
            new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Location), nameof(AddLocationToZoneSystem))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Minimap), nameof(Minimap.Awake)),
            postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Location), nameof(AddMinimapIcons))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(FejdStartup), nameof(FejdStartup.Awake)),
            new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Location), nameof(Patch_FejdStartup))));
    }

    public static class PrefabManager
    {
        private struct BundleId
        {
            [UsedImplicitly] public string AssetBundleFileName;
            [UsedImplicitly] public string FolderName;
        }

        private static readonly Dictionary<BundleId, AssetBundle> BundleCache = new();

        public static AssetBundle RegisterAssetBundle(string assetBundleFileName, string folderName = "assets")
        {
            BundleId id = new() { AssetBundleFileName = assetBundleFileName, FolderName = folderName };
            if (!BundleCache.TryGetValue(id, out var assets))
            {
                assets = BundleCache[id] =
                    Resources.FindObjectsOfTypeAll<AssetBundle>().FirstOrDefault(a => a.name == assetBundleFileName) ??
                    AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name +
                                                   $"{(folderName == "" ? "" : ".") + folderName}." +
                                                   assetBundleFileName));
            }

            return assets;
        }

        [PublicAPI]
        public static AssetID AssetIDFromObject(Object obj)
        {
            var id = obj.GetInstanceID();
            return new AssetID(1, 1, 1, (uint)id);
        }

        public static SoftReference<T> AddLoadedSoftReferenceAsset<T>(T obj) where T : Object
        {
            var bundleLoader = AssetBundleLoader.Instance;
            bundleLoader.m_bundleNameToLoaderIndex[""] = 0; // So that AssetLoader ctor doesn't crash

            var id = AssetIDFromObject(obj);
            AssetLoader loader = new(id, new AssetLocation("", ""))
            {
                m_asset = obj,
                m_referenceCounter = new ReferenceCounter(2),
                m_shouldBeLoaded = true,
            };

            var count = bundleLoader.m_assetIDToLoaderIndex.Count;
            if (count >= bundleLoader.m_assetLoaders.Length)
            {
                Array.Resize(ref bundleLoader.m_assetLoaders, bundleLoader.m_assetIDToLoaderIndex.Count + 256);
            }

            bundleLoader.m_assetLoaders[count] = loader;
            bundleLoader.m_assetIDToLoaderIndex[id] = count;

            return new SoftReference<T>(id) { m_name = obj.name };
        }
    }

    private static BaseUnityPlugin? _plugin;

    private static BaseUnityPlugin Plugin
    {
        get
        {
            if (_plugin is not null) return _plugin;
            IEnumerable<TypeInfo> types;
            try
            {
                types = Assembly.GetExecutingAssembly().DefinedTypes.ToList();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).Select(t => t.GetTypeInfo());
            }

            _plugin = (BaseUnityPlugin)Chainloader.ManagerObject.GetComponent(types.First(t =>
                t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));

            return _plugin;
        }
    }

    private static bool _hasConfigSync = true;
    private static object? _configSync;

    private static object? ConfigSync
    {
        get
        {
            if (_configSync != null || !_hasConfigSync) return _configSync;
            if (Assembly.GetExecutingAssembly().GetType("ServerSync.ConfigSync") is { } configSyncType)
            {
                _configSync = Activator.CreateInstance(configSyncType, Plugin.Info.Metadata.GUID + " ItemManager");
                configSyncType.GetField("CurrentVersion")
                    .SetValue(_configSync, Plugin.Info.Metadata.Version.ToString());
                configSyncType.GetProperty("IsLocked")!.SetValue(_configSync, true);
            }
            else
            {
                _hasConfigSync = false;
            }

            return _configSync;
        }
    }

    private static ConfigEntry<T> Config<T>(string group, string name, T value, ConfigDescription description)
    {
        var configEntry = Plugin.Config.Bind(group, name, value, description);

        ConfigSync?.GetType().GetMethod("AddConfigEntry")!.MakeGenericMethod(typeof(T))
            .Invoke(ConfigSync, [configEntry]);

        return configEntry;
    }

    private static ConfigEntry<T> Config<T>(string group, string name, T value, string description) =>
        Config(group, name, value, new ConfigDescription(description));
}