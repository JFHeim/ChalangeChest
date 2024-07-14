using BepInEx;
using ChallengeChest.Managers.LocalizationManager;
using ChallengeChest.Patch;
using fastJSON;

namespace ChallengeChest;

[BepInPlugin(ModGuid, ModName, ModVersion)]
[BepInDependency("com.Frogger.NoUselessWarnings", BepInDependency.DependencyFlags.SoftDependency)]
internal class Plugin : BaseUnityPlugin
{
    private const string ModName = "ChallengeChest",
        ModAuthor = "Frogger",
        ModVersion = "0.1.5",
        ModGuid = $"com.{ModAuthor}.{ModName}";

    public static readonly int VFXHash = "vfx_Place_workbench".GetStableHashCode();

    private void Awake()
    {
        JSON.Parameters = new JSONParameters
        {
            UseExtensions = false,
            SerializeNullValues = false,
            DateTimeMilliseconds = false,
            UseUTCDateTime = true,
            UseOptimizedDatasetSchema = true,
            UseValuesOfEnums = true,
            EnableAnonymousTypes = true,
        };

        CreateMod(this, ModName, ModAuthor, ModVersion, ModGuid);
        Localizer.Load();
        Init();

        RegisterPrefabs.LoadAll();
        bundle.Unload(false);

        EventSetup.Init();
        EventSpawn.Init();
    }
}