using BepInEx;
using ChallengeChest.Managers.LocalizationManager;
using ChallengeChest.Patch;

namespace ChallengeChest;

[BepInPlugin(ModGuid, ModName, ModVersion)]
[BepInDependency("com.Frogger.NoUselessWarnings", BepInDependency.DependencyFlags.SoftDependency)]
internal class Plugin : BaseUnityPlugin
{
    private const string ModName = "ChallengeChest",
        ModAuthor = "Frogger",
        ModVersion = "0.1.0",
        ModGuid = $"com.{ModAuthor}.{ModName}";

    private void Awake()
    {
        CreateMod(this, ModName, ModAuthor, ModVersion, ModGuid);
        Localizer.Load();
        TheConfig.Init();

        RegisterPrefabs.LoadAll();
        bundle.Unload(false);
        EventSetup.Init();
        EventSpawn.Init();
    }
}