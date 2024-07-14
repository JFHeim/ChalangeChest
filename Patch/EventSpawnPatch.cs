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
        Minimap.instance.ForceUpdateLocationPins();
    }
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
file static class CachePrefabs
{
    [HarmonyPostfix, UsedImplicitly]
    private static async void Postfix(ZNetScene __instance)
    {
        PlayerBasePieces.Clear();
        var prefabs = __instance.m_prefabs.Select(x => x).ToList();
        foreach (var prefab in prefabs)
        {
            if (!prefab) continue;
            if (prefab.GetComponentInChildren<EffectArea>(false)?.m_type != EffectArea.Type.PlayerBase)
            {
                await Task.Yield();
                continue;
            }

            PlayerBasePieces.Add(prefab.name.GetStableHashCode());
        }
    }
}

[HarmonyPatch(typeof(Game), nameof(Game.Start))]
file static class DespawnEventZdosOnWorldLoad
{
    [HarmonyPostfix, UsedImplicitly]
    private static async void Postfix()
    {
        Debug($"DespawnEventZdosOnWorldLoad 0");
        var chestHash = "cc_SuccessChest_normal".GetStableHashCode();
        await Task.Delay(10_000);
        var zdos = await ZoneSystem.instance.GetWorldObjectsAsync(
            zdo => zdo.GetVec3("ChallengeChestPos", Vector3.zero) != Vector3.zero
                   || zdo.GetPrefab() == chestHash);
        Debug($"DespawnEventZdosOnWorldLoad 1 zdos Count ={zdos.Count}");
        foreach (var zdo in zdos) ZDOMan.instance.DestroyZDO(zdo);
        Debug($"DespawnEventZdosOnWorldLoad 2");
    }
}