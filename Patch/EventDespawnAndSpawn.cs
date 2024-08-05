using HarmonyLib;
using static ChallengeChest.EventSpawn;
using static ChallengeChest.Managers.LocalizationManager.Location.PrefabManager;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
file static class EventDespawnAndSpawn
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(ZoneSystem __instance)
    {
        if (!EventSetup.locationReference.m_name.IsGood())
            EventSetup.locationReference = AddLoadedSoftReferenceAsset(EventSetup.Prefab);
        
        if (ModEnabled.Value == false) return;
        if (!ZNet.instance.IsServer()) return;

        __instance.StartCoroutine(Check());
        return;

        IEnumerator Check()
        {
            while (ZoneSystem.instance)
            {
                yield return new WaitForSeconds(5);
                if (ModEnabled.Value == false) break;

                if (EventSpawnTimer.Value <= 0)
                {
                    yield return new WaitForSeconds(1);
                    DebugError("EventSpawnTimer <= 0. Waiting for value to be > 0");
                    continue;
                }

                var oldRemainingTime = int.MaxValue;
                var remainingTime = int.MaxValue - 1;
                while (oldRemainingTime > remainingTime || oldRemainingTime > 50)
                {
                    yield return CheckDespawnEnumerator();
                    yield return new WaitForSeconds(1);
                    oldRemainingTime = remainingTime;
                    if (ZNet.instance.m_players.Count < MinimumPlayersOnline.Value) continue;
                    if (EventSpawnTimer.Value > 0)
                    {
                        remainingTime = EventSpawnTimer.Value * 60 - (int)ZNet.instance.GetTimeSeconds() %
                            (EventSpawnTimer.Value * 60);
                    }
                }

                SpawnBoss();
            }
        }
    }
}