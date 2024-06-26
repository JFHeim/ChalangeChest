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

    public static ConfigEntry<int> EventSpawnTimer { get; private set; }
    public static ConfigEntry<int> SpawnMinDistance { get; private set; }
    public static ConfigEntry<int> SpawnMaxDistance { get; private set; }
    public static ConfigEntry<int> SpawnBaseDistance { get; private set; }
    public static ConfigEntry<int> TimeLimit { get; private set; }
    public static ConfigEntry<int> MapDisplayOffset { get; private set; }

    private static readonly Dictionary<Difficulty, ConfigEntry<string>> ChestItems = [];
    public static readonly Dictionary<Difficulty, Func<List<ChestDrop>>> ChestDrops = [];

    public static void Init()
    {
        var order = 0;

        EventSpawnTimer = config("General", "EventSpawnTimer", 60,
            new ConfigDescription("Interval between ChallengeChest spawns. In minutes",
                new AcceptableValueRange<int>(1, 10080),
                new ConfigurationManagerAttributes { Order = --order, CustomDrawer = DrawTime }));

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

        MapDisplayOffset = config("Visual", "Countdown Display Offset - Label on map", 0,
            new ConfigDescription("Offset for the world boss countdown display on the world map. " +
                                  "Increase this, to move the display down, to prevent overlapping with other mods.",
                null, new ConfigurationManagerAttributes { Order = --order }), false);

        MapDisplayOffset.SettingChanged += (_, _) => EventSpawn.UpdateTimerPosition();

        foreach (var difficultyName in Enum.GetNames(typeof(Difficulty)))
        {
            var difficulty = (Difficulty)Enum.Parse(typeof(Difficulty), difficultyName);
            ChestItems.Add(difficulty, config("Chest Drop",
                $"Difficulty - {difficultyName}", "", new ConfigDescription("", null,
                    new ConfigurationManagerAttributes { CustomDrawer = DrawChestItems, Order = --order })));
            ChestDrops.Add(difficulty, () => new SerializedDrops(ChestItems[difficulty].Value).Items);
            HidedChestDrops.Add(difficultyName);
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

        var rightColumnWidth =
            (int)(configManager?.GetType()
                .GetProperty("RightColumnWidth", BindingFlags.Instance | BindingFlags.NonPublic)!.GetGetMethod(true)
                .Invoke(configManager, []) ?? 130);

        GUILayout.BeginVertical();
        if (!isHided)
        {
            foreach (var req in new SerializedDrops(cfg.BoxedValue.ToString()).Items)
            {
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();

                GUILayout.Label("Amount Min/Max:");
                var amountMin = req.AmountMin;
                if (int.TryParse(
                        GUILayout.TextField(amountMin.ToString(), new GUIStyle(GUI.skin.textField) { fixedWidth = 78 }),
                        out var newAmountMin) && newAmountMin != amountMin && !locked)
                {
                    amountMin = newAmountMin;
                    wasUpdated = true;
                }

                GUILayout.Label("/");
                var amountMax = req.AmountMax;
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
                    GUILayout.TextField(req.ItemName, new GUIStyle(GUI.skin.textField) { fixedWidth = 180 });
                var itemName = locked ? req.ItemName : newItemName;
                wasUpdated = wasUpdated || itemName != req.ItemName;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                var chance = req.ChanceToDropAny.LimitDigits(2);
                GUILayout.Label("Chance to drop: ");
                var newChance = GUILayout.HorizontalSlider(chance, 0f, 1f,
                    slider: new GUIStyle(GUI.skin.horizontalSlider) { fixedWidth = 140 },
                    thumb: new GUIStyle(GUI.skin.horizontalSliderThumb)).LimitDigits(2);
                GUILayout.Label($" {chance}%");
                var chanceToDropAny = locked ? req.ChanceToDropAny : newChance;
                wasUpdated = wasUpdated || !Approximately(chanceToDropAny, req.ChanceToDropAny);

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("|X| Remove", new GUIStyle(GUI.skin.button) { fixedWidth = 10 * 8 + 8 }) &&
                    !locked)
                    wasUpdated = true;
                else
                    newReqs.Add(new ChestDrop
                    {
                        ItemName = itemName,
                        AmountMin = amountMin,
                        AmountMax = amountMax,
                        ChanceToDropAny = chanceToDropAny
                    });
                
                if (GUILayout.Button("|+| Add", new GUIStyle(GUI.skin.button) { fixedWidth = 7 * 8 + 8 }) && !locked)
                {
                    wasUpdated = true;
                    newReqs.Add(new ChestDrop
                    {
                        ItemName = "",
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
                    ItemName = parts[0],
                    AmountMin = parts.Length > 1 && int.TryParse(parts[1], out var amountMin) ? amountMin : 1,
                    AmountMax = parts.Length > 2 && int.TryParse(parts[2], out var amountMax) ? amountMax : 1,
                    ChanceToDropAny = parts.Length > 3 && float.TryParse(parts[3], out var chance) ? chance : 1f,
                };

                return drop;
            }).ToList();
        }

        public override string ToString() =>
            string.Join(",", Items.Select(r =>
                $"{r.ItemName}:{r.AmountMin}:{r.AmountMax}:{r.ChanceToDropAny}"));
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
    public string ItemName;
    public int AmountMin;
    public int AmountMax;
    public float ChanceToDropAny;
}