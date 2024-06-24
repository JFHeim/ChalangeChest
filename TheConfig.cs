using BepInEx.Configuration;
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

    public static void Init()
    {
        EventIntervalMin = config("General", "EventIntervalMin", 1f, "");
        EventChance = config("General", "EventChance", 10,
            new ConfigDescription("In percents", new AcceptableValueRange<int>(0, 100)));
        MaxEvents = config("General", "MaxEvents", 1,
            new ConfigDescription("", new AcceptableValueRange<int>(1, 5)));
        EventRange = config("General", "EventRange", 18f,
            new ConfigDescription("", new AcceptableValueRange<float>(5f, 60)));
        EventTime = config("General", "EventTime", 1500,
            new ConfigDescription("In seconds", new AcceptableValueRange<int>(5, 86400)));
        EventSpawnTimer = config("General", "EventSpawnTimer", 0,
            new ConfigDescription("In seconds", new AcceptableValueRange<int>(0, 86400)));

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

        WorldBossCountdownDisplayOffset = config("General", "Countdown Display Offset", 0,
            new ConfigDescription(
                "Offset for the world boss countdown display on the world map. Increase this, to move the display down, to prevent overlapping with other mods."),
            false);
        
        WorldBossCountdownDisplayOffset.SettingChanged += (_, _) => EventSpawn.UpdateTimerPosition();
    }
}