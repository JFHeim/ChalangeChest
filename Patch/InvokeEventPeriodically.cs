using fastJSON;
using HarmonyLib;

namespace ChalangeChest.Patch;

[HarmonyPatch, HarmonyWrapSafe]
public class InvokeEventPeriodically
{
    private static float eventTimer = 0f;
    private static float eventRate => Game.m_eventRate;

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    [HarmonyPostfix]
    private static void RegisterRPSs() => ZRoutedRpc.instance.Register(nameof(SyncToClients), SyncToClients);

    [HarmonyPatch(typeof(RandEventSystem), nameof(RandEventSystem.FixedUpdate))]
    [HarmonyPostfix]
    private static void FixedUpdate()
    {
        float fixedDeltaTime = Time.fixedDeltaTime;
        UpdateEventSpawn(fixedDeltaTime);
    }

    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Load))]
    [HarmonyPostfix]
    private static void Load()
    {
        if (!ZNet.instance.IsServer()) return;
        currentEvents = new();
        eventTimer = 0f;

        LoadEventsData();

        Debug($"Loaded {currentEvents.Count} ChalangeChest events");
    }

    public static void UpdateSyncData()
    {
        var json = JSON.ToJSON(currentEvents);
        DebugWarning($"Saving currentEvents ChalangeChest events, json=\n{json}");
        ZoneSystem.instance.SetGlobalKey("ChalangeChestEvents_data", json);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, $"{ModName}_SyncToClients");
        DebugWarning($"Saved {currentEvents.Count} ChalangeChest events");
    }

    private static void SyncToClients(long _)
    {
        if (ZNet.instance.IsServer()) return;
        LoadEventsData();
    }

    private static void LoadEventsData()
    {
        currentEvents = JSON.ToObject<List<EventData>>(
            ZoneSystem.instance.GetOrAddGlobalKey("ChalangeChestEvents_data",
                JSON.ToJSON(new List<EventData>())));

        ShowOnMap.needsUpdate = true;
    }

    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SaveASync))]
    [HarmonyPostfix]
    private static void ZoneSystem_Save()
    {
        if (!ZNet.instance.IsServer()) return;
        UpdateSyncData();
    }


    private static void UpdateEventSpawn(float dt)
    {
        if (ZRoutedRpc.instance == null || !Minimap.instance ||
            !Game.instance ||
            !ZoneSystem.instance || !ZNet.instance ||
            !ZNet.instance.IsServer()) return;
        eventTimer += dt;
        if (eventRate <= 0) return;
        if (eventTimer <= eventIntervalMin.Value * 60 * eventRate) return;
        if (currentEvents.Count >= maxEvents.Value) return;
        eventTimer = 0;
        if (Random.value * 100 >= eventChance.Value / eventRate) return;
        StartEvent();
    }

    private static async void StartEvent()
    {
        var players = (await ZoneSystem.instance.GetWorldObjectsAsync("Player"));
        var pl = players.Random();
        if (pl == null) return;
        var pos = pl.GetPosition();
        if (currentEvents.Exists(x => pos.DistanceXZ(x.pos) < x.range)) return;
        var eventData = EventData.Create(pos);
        currentEvents.Add(eventData);
        UpdateSyncData();

        CreateWorldObjects(eventData);

        ShowOnMap.needsUpdate = true;
    }

    private static void CreateWorldObjects(EventData data)
    {
        var prefab = eventPrefabs[data.difficulty];
        var pos = data.pos;
        var rot = Quaternion.identity;

        var zObjs = Utils.GetEnabledComponentsInChildren<ZNetView>(prefab).ToList();
        var randomSpawns = Utils.GetEnabledComponentsInChildren<RandomSpawn>(prefab).ToList();

        foreach (var randomSpawn in randomSpawns) randomSpawn.Prepare();

        foreach (RandomSpawn randomSpawn in randomSpawns)
        {
            Vector3 position2 = randomSpawn.gameObject.transform.position;
            randomSpawn.Randomize(pos + rot * position2);
        }

        foreach (ZNetView znetView in zObjs)
        {
            if (!znetView.gameObject.activeSelf) continue;
            Vector3 position3 = znetView.gameObject.transform.position;
            Vector3 position4 = pos + rot * position3;
            Quaternion rotation2 = znetView.gameObject.transform.rotation;
            Quaternion rotation3 = rot * rotation2;
            GameObject instanse = Instantiate(znetView.gameObject, position4, rotation3);
            DebugWarning($"Spawned {instanse}");
        }

        foreach (var randomSpawn in randomSpawns) randomSpawn.Reset();
        foreach (var netView in zObjs) netView.gameObject.SetActive(true);

        SnapToGround.SnappAll();
    }
}