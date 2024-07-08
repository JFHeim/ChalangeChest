using HarmonyLib;
using static ChallengeChest.EventSpawn;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.RPC_LocationIcons))]
file static class UpdateLocationIcons
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix()
    {
        if (!Minimap.instance || m_localPlayer?.IsDead() != false) return;
        Minimap.instance.UpdateLocationPins(10);
    }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection)), HarmonyWrapSafe]
file static class AddRPCs
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(ZNet __instance, ZNetPeer peer)
    {
        if (!__instance.IsServer()) return;
        peer.m_rpc.Register<int, int>("ChallengeChestDone",
            (_, sectorX, sectorY) => HandleChallengeDone(new Vector2i(sectorX, sectorY)));
    }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GetLocation)), HarmonyWrapSafe]
file static class LoadEventFromSaveFile
{
    // [HarmonyPostfix, UsedImplicitly, HarmonyPatch([typeof(int)])]
    // private static void PostfixHash(int hash, ref ZoneSystem.ZoneLocation __result)
    // {
    //     //TODO: LoadEventFromSaveFile hash
    //     // if()
    // }

    [HarmonyPostfix, UsedImplicitly, HarmonyPatch([typeof(string)])]
    private static void PostfixName(string name, ref ZoneSystem.ZoneLocation __result)
    {
        if (__result != null) return;
        if (!name.StartsWith("cc_")) return;
        Debug($"LoadEventFromSaveFile name={name}");
        __result = new ZoneSystem.ZoneLocation
        {
            m_iconAlways = true,
            // m_prefabName = $"{EventData.PrefabName}__{difficulty.Value}",
            m_prefabName = name,
            m_prefab = EventData.locationReference,
        };
    }
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
file static class CachePrefabs
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(ZNetScene __instance)
    {
        PlayerBasePieces.Clear();
        foreach (var prefab in __instance.m_prefabs.Where(prefab =>
                     prefab.GetComponent<EffectArea>()?.m_type == EffectArea.Type.PlayerBase))
        {
            PlayerBasePieces.Add(prefab.name.GetStableHashCode());
        }
    }
}