using ChallengeChest.Patch;

namespace ChallengeChest;

public class EventData
{
    public static readonly Dictionary<Difficulty, EventData> Events = [];

    public readonly string PrefabName;
    public readonly GameObject Prefab;
    public readonly Difficulty Difficulty;
    public readonly Sprite Icon;
    public readonly float Range;

    public EventData(string prefabName, string iconName)
    {
        PrefabName = prefabName;
        Prefab = RegisterPrefabs.Prefab(prefabName);
        Range = Prefab.GetComponent<Location>()?.GetMaxRadius() ?? 0f;
        Icon = RegisterPrefabs.Sprite(iconName);
        Difficulty = PrefabName.GetDifficultyFromPrefab().Value;

        Events.Add(Difficulty, this);
        Debug($"Loaded EventData {Prefab?.name ?? "null"}");
    }
}