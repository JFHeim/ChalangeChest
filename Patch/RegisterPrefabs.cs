using HarmonyLib;
using static ChallengeChest.Patch.RegisterPrefabs;

namespace ChallengeChest.Patch;

public static class RegisterPrefabs
{
    public static readonly List<Object> Assets = [];
    public static readonly List<Sprite> Sprites = [];
    public static readonly List<GameObject> Prefabs = [];

    public static void LoadAll()
    {
        if (!bundle) bundle = LoadAssetBundle(ModName.ToLower());
        Assets.Clear();
        Assets.AddRange(bundle.LoadAllAssets());
        foreach (var name in Assets) Debug($"Registering {name}");
        foreach (var obj in Assets.Where(o => o is Sprite)) Sprites.Add(obj as Sprite);
        foreach (var obj in Assets.Where(o => o is GameObject)) Prefabs.Add(obj as GameObject);
    }

    public static T Asset<T>(string assetName) where T : Object =>
        Assets.FirstOrDefault(o => o.name == assetName && o is T) as T;

    public static GameObject Prefab(string prefabName) => Prefabs.FirstOrDefault(o => o.name == prefabName);
    public static Sprite Sprite(string spriteName) => Sprites.FirstOrDefault(o => o.name == spriteName);
}

[HarmonyPatch(typeof(ZNetScene)), HarmonyWrapSafe]
file static class RegisterPrefabsPatch
{
    [HarmonyPatch(nameof(ZNetScene.Awake))]
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix()
    {
        foreach (var go in Prefabs)
        {
            if (!ZNetScene.instance.m_prefabs.Contains(go))
            {
                Debug($"Adding {go} to ZNetScene prefabs");
                ZNetScene.instance.m_prefabs.Add(go);
            }

            if (!ZNetScene.instance.m_namedPrefabs.ContainsKey(go.GetPrefabName().GetStableHashCode()))
            {
                Debug($"Adding {go} to ZNetScene namedPrefabs");
                ZNetScene.instance.m_namedPrefabs.Add(go.GetPrefabName().GetStableHashCode(), go);
            }
        }
    }
}