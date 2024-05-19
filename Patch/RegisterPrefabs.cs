using HarmonyLib;

namespace ChalangeChest.Patch;

public static class RegisterPrefabs
{
    private static List<GameObject> toRegister = new();
    public static void RegisterPrefab(GameObject go) { toRegister.Add(go); }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    [HarmonyPostfix]
    private static void Awake()
    {
        foreach (var go in toRegister)
        {
            ZNetScene.instance.m_prefabs.Add(go);
            ZNetScene.instance.m_namedPrefabs.Add(go.name.GetStableHashCode(), go);
        }
    }
}