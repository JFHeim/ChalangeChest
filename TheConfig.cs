using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using ChallengeChest.Patch;

namespace ChallengeChest;

public static class TheConfig
{
    public static ConfigEntry<float> EventIntervalMin { get; private set; }
    public static ConfigEntry<int> EventChance { get; private set; }
    public static ConfigEntry<int> MaxEvents { get; private set; }
    public static ConfigEntry<float> EventRange { get; private set; }
    public static ConfigEntry<int> EventTime { get; private set; }
    public static ConfigEntry<int> EventSpawnTimer { get; private set; }
    public static ConfigEntry<int> SpawnMinDistance { get; private set; }
    public static ConfigEntry<int> SpawnMaxDistance { get; private set; }
    public static ConfigEntry<int> SpawnBaseDistance { get; private set; }
    public static ConfigEntry<int> TimeLimit { get; private set; }
    public static ConfigEntry<float> SpawnChance { get; private set; }
    public static ConfigEntry<int> WorldBossCountdownDisplayOffset { get; private set; }

    public static ConfigEntry<string> ItemsNormal { get; private set; }

    public static void Init()
    {
        var order = 0;
        EventIntervalMin = config("General", "EventIntervalMin", 1f, "");
        EventChance = config("General", "EventChance", 10,
            new ConfigDescription("In percents", new AcceptableValueRange<int>(0, 100),
                new ConfigurationManagerAttributes { Order = --order }));
        MaxEvents = config("General", "MaxEvents", 1,
            new ConfigDescription("", new AcceptableValueRange<int>(1, 5),
                new ConfigurationManagerAttributes { Order = --order }));
        EventRange = config("General", "EventRange", 18f,
            new ConfigDescription("", new AcceptableValueRange<float>(5f, 60),
                new ConfigurationManagerAttributes { Order = --order }));
        EventTime = config("General", "EventTime", 1500,
            new ConfigDescription("In seconds", new AcceptableValueRange<int>(5, 86400),
                new ConfigurationManagerAttributes { Order = --order }));
        EventSpawnTimer = config("General", "EventSpawnTimer", 0,
            new ConfigDescription("In seconds", new AcceptableValueRange<int>(0, 86400),
                new ConfigurationManagerAttributes { Order = --order }));

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
                new ConfigurationManagerAttributes { Order = --order }));

        SpawnChance = config("General", "ChallengeChest Spawn Chance", 10f,
            new ConfigDescription(
                "Chance for the ChallengeChest to spawn. Set this to 0, to disable the spawn.",
                new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { Order = --order }));

        WorldBossCountdownDisplayOffset = config("General", "Countdown Display Offset", 0,
            new ConfigDescription("Offset for the world boss countdown display on the world map. " +
                                  "Increase this, to move the display down, to prevent overlapping with other mods.",
                null, new ConfigurationManagerAttributes { Order = --order }), false);

        WorldBossCountdownDisplayOffset.SettingChanged += (_, _) => EventSpawn.UpdateTimerPosition();

        ItemsNormal = config("Difficulty - Normal", "ItemsInChest", "",
            new ConfigDescription("", null,
                new ConfigurationManagerAttributes() { CustomDrawer = DrawConfigTable, Order = --order }));
    }


    [CanBeNull] internal static object configManager;

    private static void DrawConfigTable(ConfigEntryBase cfg)
    {
        var locked = cfg.Description.Tags
            .Select(a =>
                a.GetType().Name == "ConfigurationManagerAttributes"
                    ? (bool?)a.GetType().GetField("ReadOnly")?.GetValue(a)
                    : null).FirstOrDefault(v => v != null) ?? false;

        List<Drop> newReqs = [];
        var wasUpdated = false;

        var rightColumnWidth =
            (int)(configManager?.GetType()
                .GetProperty("RightColumnWidth", BindingFlags.Instance | BindingFlags.NonPublic)!.GetGetMethod(true)
                .Invoke(configManager, []) ?? 130);

        GUILayout.BeginVertical();
        foreach (var req in new SerializedDrops((string)cfg.BoxedValue).Items)
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
            var newItemName = GUILayout.TextField(req.ItemName, new GUIStyle(GUI.skin.textField) { fixedWidth = 180 });
            var itemName = locked ? req.ItemName : newItemName;
            wasUpdated = wasUpdated || itemName != req.ItemName;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            var chance = req.ChanceToDropAny;
            GUILayout.Label($"Chance to drop: ");
            //show HorizontalSlider from 0 to 1
            var newChance = GUILayout.HorizontalSlider(chance, 0f, 1f);
            // slider: new GUIStyle(GUI.skin.horizontalSlider) { fixedWidth = 150 },
            // thumb: new GUIStyle(GUI.skin.horizontalSliderThumb));
            GUILayout.Label($" {chance}%");
            var chanceToDropAny = locked ? req.ChanceToDropAny : newChance;
            wasUpdated = wasUpdated || !Approximately(chanceToDropAny, req.ChanceToDropAny);

            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            if (GUILayout.Button("|X| Remove", new GUIStyle(GUI.skin.button) { fixedWidth = 85 }) && !locked)
            {
                wasUpdated = true;
            }
            else
            {
                newReqs.Add(new Drop
                {
                    ItemName = itemName,
                    AmountMin = amountMin,
                    AmountMax = amountMax,
                    ChanceToDropAny = chanceToDropAny
                    // , Recover = recover 
                });
            }

            if (GUILayout.Button("|+| Add", new GUIStyle(GUI.skin.button) { fixedWidth = 60 }) && !locked)
            {
                wasUpdated = true;
                newReqs.Add(new Drop
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

        GUILayout.EndVertical();

        if (wasUpdated)
        {
            cfg.BoxedValue = new SerializedDrops(newReqs).ToString();
        }
    }

    public class SerializedDrops
    {
        public readonly List<Drop> Items;

        public SerializedDrops(List<Drop> items) => Items = items;

        public SerializedDrops(string reqs)
        {
            Items = reqs.Split(',').Select(r =>
            {
                var parts = r.Split(':');
                var drop = new Drop
                {
                    ItemName = parts[0],
                    AmountMin = parts.Length > 1 && int.TryParse(parts[1], out var amountMin) ? amountMin : 1,
                    AmountMax = parts.Length > 2 && int.TryParse(parts[2], out var amountMax) ? amountMax : 1,
                    ChanceToDropAny = parts.Length > 3 && !float.TryParse(parts[3].Replace(".", ","), out var chance) ? chance : 1f,
                };
                DebugError($"{parts[3]}|{drop.ChanceToDropAny}");

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
            : BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(configManagerType);
    }
}

public struct Drop
{
    public string ItemName;
    public int AmountMin;
    public int AmountMax;
    public float ChanceToDropAny;
}