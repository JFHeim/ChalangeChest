using HarmonyLib;

namespace ChallengeChest.Patch;

[HarmonyPatch, HarmonyWrapSafe]
public static class ConfigureStaff
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    [HarmonyPostfix]
    private static void _()
    {
        // Остальные сам сделаешь
        Normal();
        // Okay();
        Good();
        // Notgood();
        // Hard();
        Impossible();
        // DeadlyPossible();
    }

    private static void SetBkg(Container chest) =>
        chest.m_bkg = ZNetScene.instance.GetPrefab("piece_chest").GetComponent<Container>().m_bkg;

    private static void Normal()
    {
        Container chest = eventPrefabs[Difficulty.Normal].transform.FindChildByName("SucsessChest").GetComponent<Container>();
        SetBkg(chest);
        chest.m_defaultItems.m_drops = [];
        chest.AddDrop("Ruby", 1, 8);
        chest.AddDrop("Sausages", 0, 4);
        chest.AddDrop("SurtlingCore", 0, 2);
        chest.AddDrop("MeadTasty", 0, 2);
    }


    private static void Good()
    {
        Container chest = eventPrefabs[Difficulty.Good].transform.FindChildByName("SucsessChest").GetComponent<Container>();
        SetBkg(chest);
        chest.AddDrop("Ruby", 3, 12);
        chest.AddDrop("Sausages", 0, 4);
        chest.AddDrop("Silver", 0, 4);
        chest.AddDrop("MeadStaminaMedium", 0, 2);
        chest.AddDrop("MeadPoisonResist", 0, 2);
    }

    private static void Impossible()
    {
        Container chest = eventPrefabs[Difficulty.Impossible].transform.FindChildByName("SucsessChest")
            .GetComponent<Container>();
        SetBkg(chest);
        chest.AddDrop("Ruby", 7, 25);
        chest.AddDrop("MeadEitrMinor", 0, 2);
        chest.AddDrop("BlackCore", 0, 4);
        chest.AddDrop("Flametal", 0, 2);
    }
}

static class Extension
{
    public static Container AddDrop(this Container chest, string name, int min, int max)
    {
        chest.m_defaultItems.m_drops ??= [];
        chest.m_defaultItems.m_drops.Add(new DropTable.DropData
        {
            m_item = ZNetScene.instance.GetPrefab(name),
            m_stackMin = min,
            m_stackMax = max,
        });
        return chest;
    }
}