using ChallengeChest.Patch;

namespace ChallengeChest;

public class EventData
{
    public readonly string PrefabName;
    public readonly GameObject Prefab;

    public EventData(string prefabName)
    {
        PrefabName = prefabName;
        Prefab = RegisterPrefabs.Prefab(prefabName);

        Debug($"Loaded EventData {Prefab?.name ?? "null"}");
    }
}