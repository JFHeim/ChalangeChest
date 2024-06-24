using BepInEx;
using BepInEx.Configuration;
using ChallengeChest.Managers.LocalizationManager;
using ChallengeChest.Patch;
using ChallengeChest.UnityScripts;
using fastJSON;

namespace ChallengeChest;

[BepInPlugin(ModGuid, ModName, ModVersion)]
[BepInDependency("com.Frogger.NoUselessWarnings", BepInDependency.DependencyFlags.SoftDependency)]
internal class Plugin : BaseUnityPlugin
{
    private const string ModName = "ChallengeChest",
        ModAuthor = "Frogger",
        ModVersion = "0.1.0",
        ModGuid = $"com.{ModAuthor}.{ModName}";

    public static ConfigEntry<float> EventIntervalMin { get; private set; }
    public static ConfigEntry<int> EventChance { get; private set; }
    public static ConfigEntry<int> MaxEvents { get; private set; }
    public static ConfigEntry<float> EventRange { get; private set; }
    public static ConfigEntry<int> EventTime { get; private set; }
    public static ConfigEntry<int> EventSpawnTimer { get; private set; }
    public static ConfigEntry<int> SpawnMinDistance { get; private set; } = null!;
    public static ConfigEntry<int> SpawnMaxDistance { get; private set; } = null!;
    public static ConfigEntry<int> SpawnBaseDistance { get; private set; } = null!;
    public static ConfigEntry<int> TimeLimit { get; private set; } = null!;
    public static ConfigEntry<float> SpawnChance { get; private set; } = null!;
    public static ConfigEntry<int> WorldBossCountdownDisplayOffset { get; private set; } = null!;

    public static Dictionary<Difficulty, DifficultyModifyer> modifiers;


    private void Awake()
    {
        CreateMod(this, ModName, ModAuthor, ModVersion, ModGuid);
        OnConfigurationChanged += () =>
        {
            Debug("Configuration changed");
            ShowOnMap.needsUpdate = true;
        };
        Localizer.Load();

        EventChance = config("General", "EventChance", 10,
            new ConfigDescription("In percents", new AcceptableValueRange<int>(0, 100)));
        EventIntervalMin = config("General", "EventIntervalMin", 1f, "");
        MaxEvents = config("General", "MaxEvents", 1,
            new ConfigDescription("", new AcceptableValueRange<int>(1, 5)));
        EventRange = config("General", "EventRange", 18f,
            new ConfigDescription("", new AcceptableValueRange<float>(5f, 60)));
        EventTime = config("General", "EventTime", 1500,
            new ConfigDescription("In seconds", new AcceptableValueRange<int>(5, 86400)));
        EventSpawnTimer = config("General", "EventSpawnTimer", 0,
            new ConfigDescription("In seconds", new AcceptableValueRange<float>(0, 86400)));

        SpawnMinDistance = config("General", "Minimum Distance ChallengeChest Spawns", 1000,
            "Minimum distance from the center of the map for ChallengeChest spawns.");
        SpawnMaxDistance = config("General", "Maximum Distance ChallengeChest Spawns", 10000,
            "Maximum distance from the center of the map for ChallengeChest spawns.");
        SpawnBaseDistance = config("General", "Base Distance ChallengeChest Spawns", 50,
            "Minimum distance to player build structures for ChallengeChest spawns.");
        TimeLimit = config("General", "Time Limit", 60, "Time in minutes before ChallengeChest despawn.");

        SpawnChance = config("General", "ChallengeChest Spawn Chance", 10f,
            new ConfigDescription(
                "Chance for the ChallengeChest to spawn. Set this to 0, to disable the spawn.",
                new AcceptableValueRange<float>(0f, 100f)));
        
        WorldBossCountdownDisplayOffset = config("General", "Countdown Display Offset", 0, new ConfigDescription("Offset for the world boss countdown display on the world map. Increase this, to move the display down, to prevent overlapping with other mods."), false);
        WorldBossCountdownDisplayOffset.SettingChanged += (_, _) => InvokeEventPeriodically.UpdateTimerPosition();

        Parameters = new JSONParameters
        {
            UseExtensions = false,
            SerializeNullValues = false,
            DateTimeMilliseconds = false,
            UseUTCDateTime = true,
            UseOptimizedDatasetSchema = true,
            UseValuesOfEnums = true
        };

        LoadAssetBundle("ChallengeChest");
        InitDifficultyMods();

        var activateChangePrefab = bundle.LoadAsset<GameObject>("ActivateChallenge");
        activateChangePrefab.GetOrAddComponent<ActivateChalange>();
        RegisterPrefabs.RegisterPrefab(activateChangePrefab);
        
        
        Character
    }

    private static void InitDifficultyMods()
    {
        modifiers = new Dictionary<Difficulty, DifficultyModifyer>(7);
        Normal();
        Okay();
        Good();
        Notgood();
        Hard();
        return;
        //TODO: Add Impossible and DeadlyPossible

        void Normal()
        {
            var mod = new DifficultyModifyer();
            mod.Monsters =
            [
                new DifficultyModifyer.MonsterMod("Greyling", spawnCh: 1, min: 2, max: 10, starCh: 0.35f,
                    star2Ch: 0.1f),
                new DifficultyModifyer.MonsterMod("Greydwarf", spawnCh: 1, min: 4, max: 8, starCh: 0.35f,
                    star2Ch: 0.2f),
                new DifficultyModifyer.MonsterMod("Greydwarf_Elite", spawnCh: 0.9f, min: 1, max: 2, starCh: 0.2f,
                    star2Ch: 0.05f),
                new DifficultyModifyer.MonsterMod("Greydwarf_Shaman", spawnCh: 0.6f, min: 0, max: 1, starCh: 0.1f,
                    star2Ch: 0.05f),
                new DifficultyModifyer.MonsterMod("Skeleton", spawnCh: 0.2f, min: 0, max: 1, starCh: 0, star2Ch: 0),
            ];

            modifiers.Add(key: Difficulty.Normal, value: mod);
        }

        void Okay()
        {
            var mod = new DifficultyModifyer();
            mod.Monsters =
            [
                new DifficultyModifyer.MonsterMod("Skeleton", min: 3, max: 6, spawnCh: 1f),
                new DifficultyModifyer.MonsterMod("Skeleton_Poison", min: 1, max: 2, spawnCh: 0.9f, starCh: 0.2f,
                    star2Ch: 0.05f),

                new DifficultyModifyer.MonsterMod("Greydwarf", starCh: 0.2f, star2Ch: 0.1f),
                new DifficultyModifyer.MonsterMod("Greydwarf_Elite", max: 2),
                new DifficultyModifyer.MonsterMod("Greydwarf_Shaman", min: 1),
            ];

            modifiers.Add(Difficulty.Okay, mod);
        }

        void Good()
        {
            var mod = new DifficultyModifyer();
            mod.Monsters =
            [
                new DifficultyModifyer.MonsterMod("Troll", min: 1, max: 1, spawnCh: 0.7f, starCh: 0.15f,
                    star2Ch: 0.07f),

                new DifficultyModifyer.MonsterMod("Skeleton", min: 5, max: 11, spawnCh: 0.8f),
                new DifficultyModifyer.MonsterMod("Skeleton_Poison", max: 3, star2Ch: 0.1f),

                new DifficultyModifyer.MonsterMod("Greydwarf", spawnCh: 0.7f, starCh: 0.4f, star2Ch: 0.2f),
                new DifficultyModifyer.MonsterMod("Greydwarf_Shaman", max: 2, starCh: 0.2f, star2Ch: 0.1f),
            ];
            mod.bannedMonsters = ["Greyling"];

            modifiers.Add(Difficulty.Good, mod);
        }

        void Notgood()
        {
            var mod = new DifficultyModifyer();
            mod.Monsters =
            [
                new DifficultyModifyer.MonsterMod("Wolf", min: 2, max: 6, spawnCh: 1, starCh: 0.2f, star2Ch: 0.1f),
                new DifficultyModifyer.MonsterMod("Draugr", min: 3, max: 8, spawnCh: 0.4f, starCh: 0.1f,
                    star2Ch: 0.05f),
                new DifficultyModifyer.MonsterMod("Troll", min: 1, max: 2, spawnCh: 0.7f, starCh: 0.15f,
                    star2Ch: 0.07f),

                new DifficultyModifyer.MonsterMod("Hatchling", min: 2, max: 6, spawnCh: 1, starCh: 0.2f, star2Ch: 0.1f),
                new DifficultyModifyer.MonsterMod("Skeleton", min: 3, max: 16, spawnCh: 0.8f, starCh: 0.3f,
                    star2Ch: 0.15f),
                new DifficultyModifyer.MonsterMod("Skeleton_Poison", spawnCh: 0.7f, max: 4, starCh: 0.2f,
                    star2Ch: 0.1f),

                new DifficultyModifyer.MonsterMod("Greydwarf_Elite", min: 2, max: 4, spawnCh: 0.8f, starCh: 0.4f,
                    star2Ch: 0.2f),
            ];
            mod.bannedMonsters = ["Greydwarf"];

            modifiers.Add(Difficulty.Notgood, mod);
        }

        void Hard()
        {
            var mod = new DifficultyModifyer();
            mod.Monsters =
            [
                new DifficultyModifyer.MonsterMod("Ulv", min: 0, max: 3, spawnCh: 0.6f, starCh: 0.2f, star2Ch: 0.1f),
                new DifficultyModifyer.MonsterMod("Draugr", max: 9, spawnCh: 0.8f),
                new DifficultyModifyer.MonsterMod("Draugr_Elite", min: 2, max: 5, spawnCh: 0.6f, starCh: 0.2f,
                    star2Ch: 0.1f),
                new DifficultyModifyer.MonsterMod("Draugr_Ranged", min: 2, max: 5, spawnCh: 0.6f, starCh: 0.2f,
                    star2Ch: 0.1f),

                new DifficultyModifyer.MonsterMod("Goblin", min: 2, max: 6, spawnCh: 1, starCh: 0.2f, star2Ch: 0.1f),
                new DifficultyModifyer.MonsterMod("GoblinArcher", min: 2, max: 6, spawnCh: 1, starCh: 0.2f,
                    star2Ch: 0.1f),
                new DifficultyModifyer.MonsterMod("GoblinBrute", min: 1, max: 3, spawnCh: 1, starCh: 0.1f,
                    star2Ch: 0.15f),
            ];
            mod.bannedMonsters = ["Greydwarf_Shaman", "Greydwarf_Elite"];

            modifiers.Add(Difficulty.Hard, mod);
        }
    }
}