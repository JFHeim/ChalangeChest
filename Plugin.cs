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


    public static readonly int VFXHash = "vfx_Place_workbench".GetStableHashCode();

    private void Awake()
    {
        CreateMod(this, ModName, ModAuthor, ModVersion, ModGuid);
        Localizer.Load();
        Init();

        RegisterPrefabs.LoadAll();
        bundle.Unload(false);
        EventSetup.Init();
        EventSpawn.Init();

        var input1 = new Vector3(1, 2, 3);
        var processor1 = input1.RoundCords();
        var processor2 = processor1.ToV2();
        var processor3 = processor2.ToV3();
        Debug($"\n\n\n\n\n\n{input1} -> {processor1} -> {processor2} -> {processor3}\n\n\n\n\n\n");
    }
}