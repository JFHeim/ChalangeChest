using HarmonyLib;

namespace ChallengeChest.Patch;

public static class RegisterPrefabs
{
    private static List<GameObject> _toRegister = [];
    public static void RegisterPrefab(GameObject go) { _toRegister.Add(go); }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    [HarmonyPostfix]
    private static void Awake()
    {
        foreach (var go in _toRegister)
        {
            ZNetScene.instance.m_prefabs.Add(go);
            ZNetScene.instance.m_namedPrefabs.Add(go.name.GetStableHashCode(), go);
        }
    }
}