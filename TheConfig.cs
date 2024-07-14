using System.Globalization;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using Biome = Heightmap.Biome;

namespace ChallengeChest;

public static class TheConfig
{
    private static readonly HashSet<(Biome biome, Difficulty difficulty)> HidedChestDrops = [];
    private static readonly HashSet<(Biome biome, Difficulty difficulty)> HidedEventMobs = [];
    private static readonly HashSet<Biome> HidedEventBiomes = [];

    public static ConfigEntry<int> EventSpawnTimer { get; private set; }
    public static ConfigEntry<int> SpawnMinDistance { get; private set; }
    public static ConfigEntry<int> SpawnMaxDistance { get; private set; }
    public static ConfigEntry<int> SpawnBaseDistance { get; private set; }
    public static ConfigEntry<int> TimeLimit { get; private set; }
    public static ConfigEntry<int> MapDisplayOffset { get; private set; }
    public static ConfigEntry<bool> EventMobDrop { get; private set; }
    public static ConfigEntry<bool> ForcePvp { get; private set; }
    public static ConfigEntry<int> MinimumPlayersOnline { get; private set; }

    private static readonly Dictionary<(Biome, Difficulty), ConfigEntry<string>> ChestDrops = [];

    private static readonly Dictionary<(Biome, Difficulty), ConfigEntry<string>> EventMobs = [];

    public static void Init()
    {
        var order = 0;

        EventSpawnTimer = config("General", "EventSpawnTimer", 60,
            new ConfigDescription("Interval between ChallengeChest spawns. In minutes",
                new AcceptableValueRange<int>(1, 10080),
                new ConfigurationManagerAttributes { Order = --order, CustomDrawer = DrawTime }));

        MinimumPlayersOnline = config("General", "MinimumPlayersOnline", 3,
            new ConfigDescription("Minimum number of players online ChallengeChest event to spawn",
                new AcceptableValueRange<int>(1, 10),
                new ConfigurationManagerAttributes { Order = --order }));

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

        var biomeNames = Enum.GetNames(typeof(Biome)).Where(x => x != "None").ToList();
        for (var i = 0; i < biomeNames.Count; i++)
        {
            var biomeName = biomeNames[i];
            var biome = (Biome)Enum.Parse(typeof(Biome), biomeName);
            foreach (var difficultyName in Enum.GetNames(typeof(Difficulty)))
            {
                var category = $"{i}. Biome - {biomeName}";
                var difficulty = (Difficulty)Enum.Parse(typeof(Difficulty), difficultyName);

                ChestDrops.Add((biome, difficulty), config(category,
                    $"{difficultyName} - Chest Drops", GetDefaultDrops(biome, difficulty),
                    new ConfigDescription(
                        "This items will be in chest", null,
                        new ConfigurationManagerAttributes { CustomDrawer = DrawChestItems, Order = --order })));

                HidedChestDrops.Add((biome, difficulty));

                EventMobs.Add((biome, difficulty), config(category,
                    $"{difficultyName} - Event Mobs", GetDefaultMobs(biome, difficulty),
                    new ConfigDescription(
                        "These mobs will be spawned\nLevel: 1 = no star, 2 = 1 star, 3 = 2 stars", null,
                        new ConfigurationManagerAttributes { CustomDrawer = DrawEventMobs, Order = --order })));
                HidedEventMobs.Add((biome, difficulty));
            }

            HidedEventBiomes.Add(biome);
        }

        UpdateBiomeHide();
    }

    // ReSharper disable once UnusedParameter.Local
    private static string GetDefaultMobs(Biome biome, Difficulty difficulty) =>
        biome switch
        {
            //TODO: Мне лень думать над дефолтными мобами в зависимости от сложности 🥺 памагитЯ плиз
            Biome.Meadows => "Greyling:5:12:1:1:3," +
                             "Greydwarf_Elite:1:1:0.3," +
                             "Boar:2:5:0.8:1:3," +
                             "Neck:1:4:0.8:1:3",
            Biome.Swamp => "Blob:2:6:1:1:3," +
                           "BlobElite:2:3:0.7:1:2," +
                           "Draugr:1:8:0.8:1:3," +
                           "Draugr_Elite:1:3:0.6:1:2," +
                           "Wraith:1:2:0.9:1:2," +
                           "Abomination:1:1:0.3",
            Biome.Mountain => "Wolf:3:6:0.8:1:3," +
                              "Hatchling:1:3:0.8:1:2," +
                              "StoneGolem:1:2:0.6," +
                              "Fenring:1:2:0.6," +
                              "Ulv:1:2:0.6," +
                              "Fenring_Cultist:1:2:0.6," +
                              "Bat:1:6:0.8:1:2," +
                              "Fenring_Cultist_Hildir:1:1:0.2",
            Biome.BlackForest => "Greydwarf:5:10:0.8:1:3," +
                                 "Greydwarf_Elite:1:3:1:1:3," +
                                 "Greydwarf_Shaman:1:3:0.8:1:3," +
                                 "Skeleton:5:10:1:0.7:3," +
                                 "Troll:1:1:0.4",
            Biome.Plains => "Goblin:2:6:1:1:3," +
                            "GoblinArcher:2:6:1:1:3," +
                            "Lox:1:3:0.7:1:2," +
                            "BlobTar:1:3:0.8:1:3," +
                            "GoblinBrute:1:3:0.6:1:2," +
                            "GoblinShaman:1:1:0.9:1:2," +
                            "GoblinBruteBros:1:1:0.3",
            Biome.AshLands => "Charred_Melee:2:6:1:1:3," +
                              "Charred_Archer:2:6:1:1:3," +
                              "charred_mage:2:6:1:1:3," +
                              "Charred_Twitcher:2:6:1:1:3," +
                              "asksvin:1:3:0.8:1:3," +
                              "morgen:1:3:0.6:1:2," +
                              "FallenValkyrie:1:1:0.3",
            Biome.Mistlands => "Seeker:2:6:1:1:3," +
                               "SeekerBrood:2:6:0.6:1:3," +
                               "Tick:2:6:0.7:1:3," +
                               "SeekerBrute:2:6:1:1:3," +
                               "Gjall:1:1:0.6",
            // Biome.DeepNorth => "",
            Biome.All => "Boar:1:1:1:1:1",
            _ => ""
        };

    // ReSharper disable once UnusedParameter.Local
    private static string GetDefaultDrops(Biome biome, Difficulty difficulty) =>
        biome switch
        {
            //TODO: Мне лень думать над дефолтным дропом в зависимости от сложности 🥺 памагитЯ плиз
            //TODO: Мне лень думать над дефолтным дропом 🥺 памагитЯ
            Biome.All => "Coins:1:25:1," +
                         "Ruby:1:25:1",
            _ => ""
        };

    public static List<ChestDrop> GetChestDrops(Biome biome, Difficulty difficulty) =>
        ChestDrops
            .Where(a => a.Key.Item2 == difficulty)
            .Where(a => a.Key.Item1 == Biome.All || a.Key.Item1 == biome)
            .Select(a => a.Value.Value)
            .Select(a => new SerializedDrops(a).Items)
            .SelectMany(a => a)
            .ToList();

    public static List<EventMob> GetEventMobs(Biome biome, Difficulty difficulty) =>
        EventMobs
            .Where(a => a.Key.Item2 == difficulty)
            .Where(a => a.Key.Item1 == Biome.All || a.Key.Item1 == biome)
            .Select(a => a.Value)
            .Select(a => new SerializedMobs(a.Value).Mobs)
            .SelectMany(a => a)
            .ToList();

    // ReSharper disable once NotAccessedField.Global
    [CanBeNull] internal static object configManager;

    private static void DrawTime(ConfigEntryBase cfg)
    {
        var locked = cfg.Description.Tags
            .Select(a =>
                a.GetType().Name == "ConfigurationManagerAttributes"
                    ? (bool?)a.GetType().GetField("ReadOnly")?.GetValue(a)
                    : null).FirstOrDefault(v => v != null) ?? false;

        // ReSharper disable once RedundantAssignment
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

        var difficultyName = cfg.Definition.Key.Split([" - Chest Drops"], StringSplitOptions.RemoveEmptyEntries)[0]
            .Replace(" ", "");
        var biomeName = cfg.Definition.Section.Split([". Biome - "], StringSplitOptions.RemoveEmptyEntries)[1]
            .Replace(" ", "");
        var difficulty = (Difficulty)Enum.Parse(typeof(Difficulty), difficultyName);
        var biome = (Biome)Enum.Parse(typeof(Biome), biomeName);
        var isHided = HidedChestDrops.Contains((biome, difficulty));
        var isBiomeHided = HidedEventBiomes.Contains(biome);

        List<ChestDrop> newReqs = [];
        var wasUpdated = false;

        GUILayout.BeginVertical();
        if (!isHided && !isBiomeHided)
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


        if (!isBiomeHided && GUILayout.Button($"|@| {(isHided ? "Show chest drop" : "Hide chest drop")}",
                new GUIStyle(GUI.skin.button) { fixedWidth = 19 * 8 + 8 }) && !locked)
        {
            if (isHided) HidedChestDrops.Remove((biome, difficulty));
            else HidedChestDrops.Add((biome, difficulty));
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


        var difficultyName = cfg.Definition.Key.Split([" - Event Mobs"], StringSplitOptions.RemoveEmptyEntries)[0]
            .Replace(" ", "");
        var biomeName = cfg.Definition.Section.Split([". Biome - "], StringSplitOptions.RemoveEmptyEntries)[1]
            .Replace(" ", "");
        var difficulty = (Difficulty)Enum.Parse(typeof(Difficulty), difficultyName);
        var biome = (Biome)Enum.Parse(typeof(Biome), biomeName);
        var isHided = HidedEventMobs.Contains((biome, difficulty));
        var isBiomeHided = HidedEventBiomes.Contains(biome);

        List<EventMob> newReqs = [];
        var wasUpdated = false;

        GUILayout.BeginVertical();
        if (!isHided && !isBiomeHided)
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

                if (GUILayout.Button("|+| Add", new GUIStyle(GUI.skin.button) { fixedWidth = 7 * 8 + 8 }) &&
                    !locked)
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

        GUILayout.BeginVertical();

        if (!isBiomeHided &&
            GUILayout.Button($"|@| {(isHided ? "Show mobs" : "Hide mobs")}",
                new GUIStyle(GUI.skin.button) { fixedWidth = 13 * 8 + 8 }) && !locked)
        {
            if (isHided) HidedEventMobs.Remove((biome, difficulty));
            else HidedEventMobs.Add((biome, difficulty));
        }

        if (difficultyName == Difficulty.DeadlyPossible.ToString() &&
            GUILayout.Button($"|@| {(isBiomeHided ? "Show biome" : "Hide biome")}",
                new GUIStyle(GUI.skin.button) { fixedWidth = 14 * 8 + 8 }) && !locked)
        {
            if (isBiomeHided) HidedEventBiomes.Remove(biome);
            else HidedEventBiomes.Add(biome);

            UpdateBiomeHide();
        }

        GUILayout.EndVertical();
        GUILayout.EndVertical();

        if (wasUpdated) cfg.BoxedValue = new SerializedMobs(newReqs).ToString();
    }

    private static void UpdateBiomeHide()
    {
        foreach (var pair in EventMobs)
            ProcessCfg(pair.Value, pair.Key.Item1, pair.Key.Item2);
        foreach (var pair in ChestDrops)
            ProcessCfg(pair.Value, pair.Key.Item1, pair.Key.Item2, true);

        //call method BuildSettingList with no args
        configManager?.GetType().GetMethod("BuildSettingList")?.Invoke(configManager, null);
        return;

        void ProcessCfg(ConfigEntry<string> cfg, Biome biome, Difficulty difficulty, bool isChest = false)
        {
            var isBiomeHided = HidedEventBiomes.Contains(biome);
            if (cfg.Description.Tags.Length <= 0) return;
            var tag = cfg.Description.Tags.ToList().OfType<ConfigurationManagerAttributes>().FirstOrDefault();
            if (tag is null) return;
            if (difficulty != Difficulty.DeadlyPossible || isChest) tag.Browsable = !isBiomeHided;
            if (!isChest) tag.DispName = isBiomeHided ? "" : null;
        }
    }
}

file class SerializedDrops
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

file class SerializedMobs
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