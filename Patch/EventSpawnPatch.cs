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

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.RPC_GlobalKeys))]
file static class UpdateGlobalKeys
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix()
    {
        Debug($"RPC_GlobalKeys EventDatas.Count={EventDatas.Count}");
        LoadEventData();
    }
}

// [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection)), HarmonyWrapSafe]
// file static class AddRPCs
// {
//     [HarmonyPostfix, UsedImplicitly]
//     private static void Postfix(ZNet __instance, ZNetPeer peer)
//     {
//         if (!__instance.IsServer()) return;
//         peer.m_rpc.Register<int, int>("ChallengeChestDone",
//             (_, sectorX, sectorY) => HandleChallengeDone(new Vector2(sectorX, sectorY)));
//     }
// }

[HarmonyWrapSafe]
file static class FixLoadRegisterLocation
{
    private static ZoneSystem.ZoneLocation _lastLocation = null;
    private static bool _isEventDatasLoaded = true;

    [HarmonyPatch(typeof(ZoneSystem))]
    private static class DetectFirstRun
    {
        [HarmonyPatch(nameof(ZoneSystem.Awake))]
        [HarmonyPatch(nameof(ZoneSystem.OnDestroy))]
        [HarmonyPostfix, UsedImplicitly]
        private static void Postfix() => _isEventDatasLoaded = true;
    }

    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GetLocation))]
    [HarmonyPostfix, UsedImplicitly, HarmonyPatch([typeof(string)])]
    private static void PostfixName(string name, ref ZoneSystem.ZoneLocation __result)
    {
        if (__result != null) return;
        if (!name.StartsWith("cc_")) return;
        Debug($"LoadEventFromSaveFile 0 name={name}");
        _lastLocation = __result = new ZoneSystem.ZoneLocation
        {
            m_iconAlways = true,
            m_prefabName = name,
            m_prefab = EventSetup.locationReference,
        };
    }

    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.RegisterLocation))]
    [HarmonyPrefix, UsedImplicitly]
    private static bool PrefixRegister(ZoneSystem.ZoneLocation location, Vector3 pos)
    {
        if (_isEventDatasLoaded)
        {
            _isEventDatasLoaded = false;
                    LoadEventData();
            var key = ZoneSystem.instance.GetGlobalKeyValue($"{ModName}_Events");
            Debug($"{ModName}_Events={key}");
        }

        if (location != _lastLocation) return true;
        Debug($"LoadEventFromSaveFile 1 name={location.m_prefabName}");

        var data = EventDatas.Find(x => x.pos.Equals(pos.ToV2(), 4));
        if (data is null)
        {
            DebugError($"LoadEventFromSaveFile Could not find data for location on {pos}");
            return false;
        }

        var locationInstance = new ZoneSystem.LocationInstance
        {
            m_location = location,
            m_position = data.pos.ToVector2().ToV3(),
            m_placed = true
        };

        ZoneSystem.instance.m_locationInstances.Add(data.GetZone(), locationInstance);
        Debug($"LoadEventFromSaveFile AddedLocationPin data={data}");

        return false;
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