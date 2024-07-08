using ChallengeChest.Patch;
using SoftReferenceableAssets;

namespace ChallengeChest;

public static class EventData
{
    public const string PrefabName = "cc_EventPrefab";
    public static GameObject Prefab;
    public static SoftReference<GameObject> locationReference = new();
    public static Location Location;
    public static readonly Dictionary<Heightmap.Biome, Sprite> Icons = [];
    public static float Range = 30;

    public static void Init()
    {
        Debug("Initializing EventData...");
        Prefab = RegisterPrefabs.Prefab(PrefabName);
        Location = Prefab.GetComponent<Location>();
        Range = Location?.GetMaxRadius() ?? 10f;
        SetupIcons();

        Debug("Initialized EventData");
    }

    private static void SetupIcons()
    {
        AddIcon(Heightmap.Biome.Meadows);
        AddIcon(Heightmap.Biome.Swamp);
        AddIcon(Heightmap.Biome.Mountain);
        AddIcon(Heightmap.Biome.BlackForest);
        AddIcon(Heightmap.Biome.Plains);
        AddIcon(Heightmap.Biome.AshLands);
        AddIcon(Heightmap.Biome.DeepNorth);
        AddIcon(Heightmap.Biome.Ocean);
        AddIcon(Heightmap.Biome.Mistlands);
        return;

        void AddIcon(Heightmap.Biome biome)
        {
            var spriteName = $"cc_Icon{biome.ToString()}";
            Icons.Add(biome, RegisterPrefabs.Sprite(spriteName));
        }
    }
}