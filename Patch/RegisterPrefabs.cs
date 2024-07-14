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
        if (!bundle)
        {
            // var path = Path.Combine(Paths.PluginPath, $"{ModName}_Assets", $"{ModName.ToLower()}.bundle");
            // if (!File.Exists(path))
            // {
            //     GetPlugin().Logger.LogFatal($"No asset bundle found! Should be at {path}");
            //     return;
            // }
            //
            // bundle = AssetBundle.LoadFromFile(path);

            LoadAssetBundle(ModName.ToLower());
        }

        Assets.Clear();
        Assets.AddRange(bundle.LoadAllAssets());
        foreach (var name in Assets) Debug($"Registering {name}");
        foreach (var obj in Assets.Where(o => o is Texture2D))
        {
            var texture2D = (obj as Texture2D)!;
            var sprite = UnityEngine.Sprite.Create(texture2D,
                new Rect(0, 0, texture2D.width, texture2D.height), Vector2.zero);
            sprite.name = obj.name;
            Sprites.Add(sprite);
        }

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