using System.Globalization;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using ChallengeChest.Patch;
using HarmonyLib;

namespace ChallengeChest;

public static class TheConfig
{
    private static readonly HashSet<string> HidedChestDrops = [];
    private static readonly HashSet<string> HidedEventMobs = [];

    public static ConfigEntry<int> EventSpawnTimer { get; private set; }
    public static ConfigEntry<int> SpawnMinDistance { get; private set; }
    public static ConfigEntry<int> SpawnMaxDistance { get; private set; }
    public static ConfigEntry<int> SpawnBaseDistance { get; private set; }
    public static ConfigEntry<int> TimeLimit { get; private set; }
    public static ConfigEntry<int> MapDisplayOffset { get; private set; }
    public static ConfigEntry<bool> EventMobDrop { get; private set; }
    public static ConfigEntry<bool> ForcePvp { get; private set; }

    private static readonly Dictionary<Difficulty, ConfigEntry<string>> _chestDrops = [];
    public static readonly Dictionary<Difficulty, Func<List<ChestDrop>>> ChestDrops = [];

    private static readonly Dictionary<Difficulty, ConfigEntry<string>> _eventMobs = [];
    public static readonly Dictionary<Difficulty, Func<List<EventMob>>> EventMobs = [];

    public static void Init()
    {
        var order = 0;

        EventSpawnTimer = config("General", "EventSpawnTimer", 60,
            new ConfigDescription("Interval between ChallengeChest spawns. In minutes",
                new AcceptableValueRange<int>(1, 10080),
                new ConfigurationManagerAttributes { Order = --order, CustomDrawer = DrawTime }));

        EventMobDrop = config("General", "Event mobs drop loot", false,
            new ConfigDescription("Should ChallengeChest mobs drop loot",
                null, new ConfigurationManagerAttributes { Order = --order }));

        SpawnMinDistance = config("General", "Minimum Distance ChallengeChest Spawns", 1000,
            new ConfigDescription("Minimum distance from the center of the map for ChallengeChest spawns.", null,
                new ConfigurationManagerAttributes { Order = --order }));
        SpawnMaxDistance = config("General", "Maximum Distance ChallengeChest Spawns", 10000,
            new ConfigDescription("Maximum distance from the center of the map for ChallengeChest spawns.", null,
                new ConfigurationManagerAttributes { Order = --order }));
        SpawnBaseDistance = config("General", "Base Distance ChallengeChest Spawns", 50,
            new ConfigDescription("Minimum distance to player build structures for ChallengeChest spawns.", null,
                new ConfigurationManagerAttributes { Order = --order }));
        TimeLimit = config("General", "Time Limit", 60,
            new ConfigDescription("Time in minutes before ChallengeChest despawn.", null,
                new ConfigurationManagerAttributes { Order = --order, CustomDrawer = DrawTime }));
        ForcePvp = config("General", "ForcePvp", true,
            new ConfigDescription("Force Pvp in ChallengeChest area", null,
                new ConfigurationManagerAttributes { Order = --order }));

        MapDisplayOffset = config("Visual", "Countdown Display Offset - Label on map", 0,
            new ConfigDescription("Offset for the world boss countdown display on the world map. " +
                                  "Increase this, to move the display down, to prevent overlapping with other mods.",
                null, new ConfigurationManagerAttributes { Order = --order }), false);

        MapDisplayOffset.SettingChanged += (_, _) => EventSpawn.UpdateTimerPosition();

        foreach (var difficultyName in Enum.GetNames(typeof(Difficulty)))
        {
            var difficulty = (Difficulty)Enum.Parse(typeof(Difficulty), difficultyName);
            _chestDrops.Add(difficulty, config("Chest Drop",
                $"Difficulty - {difficultyName}", "Coin:1:2:1", new ConfigDescription(
                    "This items will be in chest", null,
                    new ConfigurationManagerAttributes { CustomDrawer = DrawChestItems, Order = --order })));
            ChestDrops.Add(difficulty, () => new SerializedDrops(_chestDrops[difficulty].Value).Items);
            HidedChestDrops.Add(difficultyName);

            _eventMobs.Add(difficulty, config("Event Mobs",
                $"Difficulty - {difficultyName}", "Goblin:5:15:1:1:3", new ConfigDescription(
                    "These mobs will be spawned\nLevel: 1 = no star, 2 = 1 star, 3 = 2 stars", null,
                    new ConfigurationManagerAttributes { CustomDrawer = DrawEventMobs, Order = --order })));
            EventMobs.Add(difficulty, () => new SerializedMobs(_eventMobs[difficulty].Value).Mobs);
            HidedEventMobs.Add(difficultyName);
        }
    }

    [CanBeNull] internal static object configManager;

    private static void DrawTime(ConfigEntryBase cfg)
    {
        var locked = cfg.Description.Tags
            .Select(a =>
                a.GetType().Name == "ConfigurationManagerAttributes"
                    ? (bool?)a.GetType().GetField("ReadOnly")?.GetValue(a)
                    : null).FirstOrDefault(v => v != null) ?? false;

        var wasUpdated = false;
        var time = TimeSpan.FromMinutes(float.Parse(cfg.BoxedValue.ToString()));

        GUILayout.BeginHorizontal();

        var oldValue = time.TotalMinutes.ToString(CultureInfo.InvariantCulture);
        var newValue = GUILayout.TextField(oldValue, new GUIStyle(GUI.skin.textField));
        var result = locked ? oldValue : newValue;
        wasUpdated = result != oldValue;
        GUILayout.Label(time.ToHumanReadableString(), new GUIStyle(GUI.skin.label) { fixedWidth = 120 });

        GUILayout.EndHorizontal();

        if (wasUpdated) cfg.BoxedValue = (int)float.Parse(result);
    }

    private static void DrawChestItems(ConfigEntryBase cfg)
    {
        var locked = cfg.Description.Tags
            .Select(a =>
                a.GetType().Name == "ConfigurationManagerAttributes"
                    ? (bool?)a.GetType().GetField("ReadOnly")?.GetValue(a)
                    : null).FirstOrDefault(v => v != null) ?? false;


        var difficultyName = cfg.Definition.Key.Replace("Difficulty - ", "");
        var isHided = HidedChestDrops.Contains(difficultyName);

        List<ChestDrop> newReqs = [];
        var wasUpdated = false;

        GUILayout.BeginVertical();
        if (!isHided)
        {
            foreach (var item in new SerializedDrops(cfg.BoxedValue.ToString()).Items)
            {
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();

                GUILayout.Label("Amount Min/Max:");
                var amountMin = item.AmountMin;
                if (int.TryParse(
                        GUILayout.TextField(amountMin.ToString(), new GUIStyle(GUI.skin.textField) { fixedWidth = 78 }),
                        out var newAmountMin) && newAmountMin != amountMin && !locked)
                {
                    amountMin = newAmountMin;
                    wasUpdated = true;
                }

                GUILayout.Label("/");
                var amountMax = item.AmountMax;
                if (int.TryParse(
                        GUILayout.TextField(amountMax.ToString(), new GUIStyle(GUI.skin.textField) { fixedWidth = 78 }),
                        out var newAmountMax) && newAmountMax != amountMax && !locked)
                {
                    amountMax = newAmountMax;
                    wasUpdated = true;
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Item Name:");
                var newItemName =
                    GUILayout.TextField(item.PrefabName, new GUIStyle(GUI.skin.textField) { fixedWidth = 180 });
                var itemName = locked ? item.PrefabName : newItemName;
                wasUpdated = wasUpdated || itemName != item.PrefabName;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                var chance = item.ChanceToDropAny.LimitDigits(2);
                GUILayout.Label("Chance to drop: ");
                var newChance = GUILayout.HorizontalSlider(chance, 0f, 1f,
                    slider: new GUIStyle(GUI.skin.horizontalSlider) { fixedWidth = 140 },
                    thumb: new GUIStyle(GUI.skin.horizontalSliderThumb)).LimitDigits(2);
                GUILayout.Label($" {chance}%");
                var chanceToDropAny = locked ? item.ChanceToDropAny : newChance;
                wasUpdated = wasUpdated || !Approximately(chanceToDropAny, item.ChanceToDropAny);

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("|X| Remove", new GUIStyle(GUI.skin.button) { fixedWidth = 10 * 8 + 8 }) &&
                    !locked)
                    wasUpdated = true;
                else
                    newReqs.Add(new ChestDrop
                    {
                        PrefabName = itemName,
                        AmountMin = amountMin,
                        AmountMax = amountMax,
                        ChanceToDropAny = chanceToDropAny
                    });

                if (GUILayout.Button("|+| Add", new GUIStyle(GUI.skin.button) { fixedWidth = 7 * 8 + 8 }) && !locked)
                {
                    wasUpdated = true;
                    newReqs.Add(new ChestDrop
                    {
                        PrefabName = "",
                        AmountMin = 1,
                        AmountMax = 1,
                        ChanceToDropAny = 1,
                        // , Recover = false 
                    });
                }

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Button($"|@| {(isHided ? "Show chest drop" : "Hide")}",
                new GUIStyle(GUI.skin.button) { fixedWidth = isHided ? 19 * 8 + 8 : 8 * 8 + 8 }) && !locked)
        {
            if (isHided) HidedChestDrops.Remove(difficultyName);
            else HidedChestDrops.Add(difficultyName);
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (wasUpdated) cfg.BoxedValue = new SerializedDrops(newReqs).ToString();
    }

    private static void DrawEventMobs(ConfigEntryBase cfg)
    {
        var locked = cfg.Description.Tags
            .Select(a =>
                a.GetType().Name == "ConfigurationManagerAttributes"
                    ? (bool?)a.GetType().GetField("ReadOnly")?.GetValue(a)
                    : null).FirstOrDefault(v => v != null) ?? false;


        var difficultyName = cfg.Definition.Key.Replace("Difficulty - ", "");
        var isHided = HidedEventMobs.Contains(difficultyName);

        List<EventMob> newReqs = [];
        var wasUpdated = false;

        GUILayout.BeginVertical();
        if (!isHided)
        {
            foreach (var mob in new SerializedMobs(cfg.BoxedValue.ToString()).Mobs)
            {
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Prefab:");
                var prefabStyle = new GUIStyle(GUI.skin.textField);
                prefabStyle.fixedWidth = 180;
                var newPrefab = GUILayout.TextField(mob.PrefabName, prefabStyle);
                var prefab = locked ? mob.PrefabName : newPrefab;
                wasUpdated = wasUpdated || prefab != mob.PrefabName;
                //TODO: textColor doesn't work so far
                if (wasUpdated)
                    prefabStyle.normal.textColor =
                        ZNetScene.instance.GetPrefab(prefab) == null ? Color.red : Color.black;

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Amount Min/Max:");
                var amountMin = mob.AmountMin;
                var amountMinStyle = new GUIStyle(GUI.skin.textField) { fixedWidth = 78 };
                if (int.TryParse(
                        GUILayout.TextField(amountMin.ToString(), amountMinStyle),
                        out var newAmountMin) && newAmountMin != amountMin && !locked)
                {
                    amountMin = newAmountMin;
                    wasUpdated = true;
                }

                if (wasUpdated)
                    amountMinStyle.normal.textColor = amountMin < 0 ? Color.red : Color.black;

                GUILayout.Label("/");
                var amountMax = mob.AmountMax;
                var amountMaxStyle = new GUIStyle(GUI.skin.textField) { fixedWidth = 78 };
                if (int.TryParse(
                        GUILayout.TextField(amountMax.ToString(), amountMaxStyle),
                        out var newAmountMax) && newAmountMax != amountMax && !locked)
                {
                    amountMax = newAmountMax;
                    wasUpdated = true;
                }

                if (wasUpdated)
                    amountMaxStyle.normal.textColor = amountMax < 0 ? Color.red : Color.black;

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                var chance = mob.ChanceToSpawnAny.LimitDigits(2);
                GUILayout.Label("Chance to spawn: ");
                var newChance = GUILayout.HorizontalSlider(chance, 0f, 1f,
                    slider: new GUIStyle(GUI.skin.horizontalSlider) { fixedWidth = 140 },
                    thumb: new GUIStyle(GUI.skin.horizontalSliderThumb)).LimitDigits(2);
                var chanceStyle = new GUIStyle(GUI.skin.label);
                GUILayout.Label($" {chance}%", chanceStyle);
                var chanceToSpawnAny = locked ? mob.ChanceToSpawnAny : newChance;
                wasUpdated = wasUpdated || !Approximately(chanceToSpawnAny, mob.ChanceToSpawnAny);

                if (wasUpdated)
                    chanceStyle.normal.textColor = chanceToSpawnAny is < 0 or > 1 ? Color.red : Color.black;

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Level Min/Max:");
                var levelMin = mob.LevelMin;
                var levelMinStyle = new GUIStyle(GUI.skin.textField) { fixedWidth = 78 };
                if (int.TryParse(
                        GUILayout.TextField(levelMin.ToString(), levelMinStyle),
                        out var newLevelMin) && newLevelMin != levelMin && !locked)
                {
                    levelMin = newLevelMin;
                    wasUpdated = true;
                }

                if (wasUpdated)
                    levelMinStyle.normal.textColor = levelMin < 0 ? Color.red : Color.black;

                GUILayout.Label("/");
                var levelMax = mob.LevelMax;
                var levelMaxStyle = new GUIStyle(GUI.skin.textField) { fixedWidth = 78 };
                if (int.TryParse(
                        GUILayout.TextField(levelMax.ToString(), levelMaxStyle),
                        out var newLevelMax) && newLevelMax != levelMax && !locked)
                {
                    levelMax = newLevelMax;
                    wasUpdated = true;
                }

                if (wasUpdated)
                    levelMaxStyle.normal.textColor = levelMax < 0 ? Color.red : Color.black;

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("|X| Remove", new GUIStyle(GUI.skin.button) { fixedWidth = 10 * 8 + 8 }) &&
                    !locked)
                    wasUpdated = true;
                else
                    newReqs.Add(new EventMob
                    {
                        PrefabName = prefab,
                        AmountMin = amountMin,
                        AmountMax = amountMax,
                        ChanceToSpawnAny = chanceToSpawnAny,
                        LevelMin = levelMin,
                        LevelMax = levelMax,
                    });

                if (GUILayout.Button("|+| Add", new GUIStyle(GUI.skin.button) { fixedWidth = 7 * 8 + 8 }) && !locked)
                {
                    wasUpdated = true;
                    newReqs.Add(new EventMob
                    {
                        PrefabName = "",
                        AmountMin = 1,
                        AmountMax = 1,
                        ChanceToSpawnAny = 1f,
                        LevelMin = 1,
                        LevelMax = 3,
                    });
                }

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Button($"|@| {(isHided ? "Show" : "Hide")}",
                new GUIStyle(GUI.skin.button) { fixedWidth = isHided ? 8 * 8 + 8 : 8 * 8 + 8 }) && !locked)
        {
            if (isHided) HidedEventMobs.Remove(difficultyName);
            else HidedEventMobs.Add(difficultyName);
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (wasUpdated) cfg.BoxedValue = new SerializedMobs(newReqs).ToString();
    }

    private class SerializedDrops
    {
        public readonly List<ChestDrop> Items;

        public SerializedDrops(List<ChestDrop> items) => Items = items;

        public SerializedDrops(string reqs)
        {
            Items = reqs.Split(',').Select(r =>
            {
                var parts = r.Split(':');
                var drop = new ChestDrop
                {
                    PrefabName = parts[0],
                    AmountMin = parts.Length > 1 && int.TryParse(parts[1], out var amountMin) ? amountMin : 1,
                    AmountMax = parts.Length > 2 && int.TryParse(parts[2], out var amountMax) ? amountMax : 1,
                    ChanceToDropAny = parts.Length > 3 && float.TryParse(parts[3], out var chance) ? chance : 1f,
                };

                return drop;
            }).ToList();
        }

        public override string ToString() =>
            string.Join(",", Items.Select(r =>
                $"{r.PrefabName}:{r.AmountMin}:{r.AmountMax}:{r.ChanceToDropAny}"));
    }

    private class SerializedMobs
    {
        public readonly List<EventMob> Mobs;

        public SerializedMobs(List<EventMob> mobs) => Mobs = mobs;

        public SerializedMobs(string reqs)
        {
            Mobs = reqs.Split(',').Select(r =>
            {
                var parts = r.Split(':');
                var drop = new EventMob
                {
                    PrefabName = parts[0],
                    AmountMin = parts.Length > 1 && int.TryParse(parts[1], out var amountMin) ? amountMin : 1,
                    AmountMax = parts.Length > 2 && int.TryParse(parts[2], out var amountMax) ? amountMax : 1,
                    ChanceToSpawnAny = parts.Length > 3 && float.TryParse(parts[3], out var chance) ? chance : 1f,
                    LevelMin = parts.Length > 4 && int.TryParse(parts[4], out var levelMin) ? levelMin : 1,
                    LevelMax = parts.Length > 5 && int.TryParse(parts[5], out var levelMax) ? levelMax : 2
                };

                return drop;
            }).ToList();
        }

        public override string ToString() =>
            string.Join(",", Mobs.Select(r =>
                $"{r.PrefabName}:{r.AmountMin}:{r.AmountMax}:" +
                $"{r.ChanceToSpawnAny}:{r.LevelMin}:{r.LevelMax}"));
    }
}

[HarmonyWrapSafe, HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
file static class FindConfigManager
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(FejdStartup __instance)
    {
        var bepinexConfigManager = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "ConfigurationManager");
        var configManagerType = bepinexConfigManager?.GetType("ConfigurationManager.ConfigurationManager");
        configManager = configManagerType == null
            ? null
            : Chainloader.ManagerObject.GetComponent(configManagerType);
    }
}

public struct ChestDrop
{
    public string PrefabName;
    public int AmountMin;
    public int AmountMax;
    public float ChanceToDropAny;
}

public struct EventMob
{
    public string PrefabName;
    public int AmountMin;
    public int AmountMax;
    public float ChanceToSpawnAny;
    public int LevelMin;
    public int LevelMax;

    //TODO: mob boosts like more or less health, more or less damage etc.
}