using BepInEx;
using BepInEx.Configuration;
using ChalangeChest.Compatibility.ESP;
using ChalangeChest.LocalizationManager;
using ChalangeChest.Patch;
using ChalangeChest.UnityScripts;
using fastJSON;

namespace ChalangeChest;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInDependency("com.Frogger.NoUselessWarnings", BepInDependency.DependencyFlags.SoftDependency)]
internal class Plugin : BaseUnityPlugin
{
    private const string ModName = "ChalangeChest",
        ModAuthor = "Frogger",
        ModVersion = "0.1.0",
        ModGUID = $"com.{ModAuthor}.{ModName}";

    public static List<EventData> currentEvents = new();
    public static ConfigEntry<float> eventIntervalMin { get; private set; }
    public static ConfigEntry<int> eventChance { get; private set; }
    public static ConfigEntry<int> maxEvents { get; private set; }
    public static ConfigEntry<float> eventRange { get; private set; }
    public static ConfigEntry<float> eventTime { get; private set; }

    public static Dictionary<Difficulty, DifficultyModifyer> modifiers = null;


    private void Awake()
    {
        CreateMod(this, ModName, ModAuthor, ModVersion, ModGUID);
        OnConfigurationChanged += () =>
        {
            Debug($"Configuration changed");
            ShowOnMap.needsUpdate = true;
        };
        Localizer.Load();

        eventChance = config("General", "EventChance", 10,
            new ConfigDescription("In percents", new AcceptableValueRange<int>(0, 100)));
        eventIntervalMin = config("General", "EventIntervalMin", 1f, "");
        maxEvents = config("General", "MaxEvents", 1,
            new ConfigDescription("", new AcceptableValueRange<int>(1, 5)));
        eventRange = config("General", "EventRange", 18f,
            new ConfigDescription("", new AcceptableValueRange<float>(5f, 60)));
        eventTime = config("General", "EventTime", 15f,
            new ConfigDescription("In minutes", new AcceptableValueRange<float>(0.1f, 120)));

        JSON.Parameters = new()
        {
            UseExtensions = false,
            SerializeNullValues = false,
            DateTimeMilliseconds = false,
            UseUTCDateTime = true,
            UseOptimizedDatasetSchema = true,
            UseValuesOfEnums = true
        };

        LoadAssetBundle("chalangechest");
        InitDifficultyMods();

        var activateChalangePrefab = bundle.LoadAsset<GameObject>("ActivateChalange");
        activateChalangePrefab.GetOrAddComponent<ActivateChalange>();
        RegisterPrefabs.RegisterPrefab(activateChalangePrefab);
    }

    private void InitDifficultyMods()
    {
        modifiers = new(7);
        Normal();
        Okay();
        Good();
        Notgood();
        Hard();
        //TODO: Add Impossible and DeadlyPossible

        void Normal()
        {
            var mod = new DifficultyModifyer();
            mod.monsters =
            [
                new("Greyling", spawnCh: 1, min: 2, max: 10, starCh: 0.35f, star2Ch: 0.1f),
                new("Greydwarf", spawnCh: 1, min: 4, max: 8, starCh: 0.35f, star2Ch: 0.2f),
                new("Greydwarf_Elite", spawnCh: 0.9f, min: 1, max: 2, starCh: 0.2f, star2Ch: 0.05f),
                new("Greydwarf_Shaman", spawnCh: 0.6f, min: 0, max: 1, starCh: 0.1f, star2Ch: 0.05f),
                new("Skeleton", spawnCh: 0.2f, min: 0, max: 1, starCh: 0, star2Ch: 0),
            ];

            modifiers.Add(key: Difficulty.Normal, value: mod);
        }

        void Okay()
        {
            var mod = new DifficultyModifyer();
            mod.monsters =
            [
                new("Skeleton", min: 3, max: 6, spawnCh: 1f),
                new("Skeleton_Poison", min: 1, max: 2, spawnCh: 0.9f, starCh: 0.2f,
                    star2Ch: 0.05f),

                new("Greydwarf", starCh: 0.2f, star2Ch: 0.1f),
                new("Greydwarf_Elite", max: 2),
                new("Greydwarf_Shaman", min: 1),
            ];

            modifiers.Add(Difficulty.Okay, mod);
        }

        void Good()
        {
            var mod = new DifficultyModifyer();
            mod.monsters =
            [
                new("Troll", min: 1, max: 1, spawnCh: 0.7f, starCh: 0.15f, star2Ch: 0.07f),

                new("Skeleton", min: 5, max: 11, spawnCh: 0.8f),
                new("Skeleton_Poison", max: 3, star2Ch: 0.1f),

                new("Greydwarf", spawnCh: 0.7f, starCh: 0.4f, star2Ch: 0.2f),
                new("Greydwarf_Shaman", max: 2, starCh: 0.2f, star2Ch: 0.1f),
            ];
            mod.bannedMonsters = ["Greyling"];

            modifiers.Add(Difficulty.Good, mod);
        }

        void Notgood()
        {
            var mod = new DifficultyModifyer();
            mod.monsters =
            [
                new("Wolf", min:2,  max: 6, spawnCh: 1, starCh: 0.2f, star2Ch: 0.1f),
                new("Draugr", min: 3, max: 8, spawnCh: 0.4f, starCh: 0.1f, star2Ch: 0.05f),
                new("Troll", min: 1, max: 2, spawnCh: 0.7f, starCh: 0.15f, star2Ch: 0.07f),

                new("Hatchling", min:2,  max: 6, spawnCh: 1, starCh: 0.2f, star2Ch: 0.1f),
                new("Skeleton", min: 3, max: 16, spawnCh: 0.8f, starCh: 0.3f, star2Ch: 0.15f),
                new("Skeleton_Poison", spawnCh: 0.7f, max: 4, starCh: 0.2f, star2Ch: 0.1f),

                new("Greydwarf_Elite", min: 2, max: 4, spawnCh: 0.8f, starCh: 0.4f, star2Ch: 0.2f),
            ];
            mod.bannedMonsters = ["Greydwarf"];

            modifiers.Add(Difficulty.Notgood, mod);
        }

        void Hard()
        {
            var mod = new DifficultyModifyer();
            mod.monsters =
            [
                new("Ulv", min:0,  max: 3, spawnCh: 0.6f, starCh: 0.2f, star2Ch: 0.1f),
                new("Draugr",  max: 9, spawnCh: 0.8f),
                new("Draugr_Elite", min:2,  max: 5, spawnCh: 0.6f, starCh: 0.2f, star2Ch: 0.1f),
                new("Draugr_Ranged", min:2,  max: 5, spawnCh: 0.6f, starCh: 0.2f, star2Ch: 0.1f),
                
                new("Goblin", min:2,  max: 6, spawnCh: 1, starCh: 0.2f, star2Ch: 0.1f),
                new("GoblinArcher", min:2,  max: 6, spawnCh: 1, starCh: 0.2f, star2Ch: 0.1f),
                new("GoblinBrute", min:1,  max: 3, spawnCh: 1, starCh: 0.1f, star2Ch: 0.15f),
            ];
            mod.bannedMonsters = ["Greydwarf_Shaman", "Greydwarf_Elite"];

            modifiers.Add(Difficulty.Hard, mod);
        }
    }
}