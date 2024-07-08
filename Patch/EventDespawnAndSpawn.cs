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
        if (!EventData.locationReference.m_name.IsGood())
            EventData.locationReference = AddLoadedSoftReferenceAsset(EventData.Prefab);

        if (!ZNet.instance.IsServer()) return;

        __instance.StartCoroutine(Check());
        return;

        IEnumerator Check()
        {
            while (ZoneSystem.instance)
            {
                yield return new WaitForSeconds(5);

                if (EventSpawnTimer.Value <= 0)
                {
                    yield return new WaitForSeconds(1);
                    continue;
                }

                var oldRemainingTime = int.MaxValue;
                var remainingTime = int.MaxValue - 1;
                while (oldRemainingTime > remainingTime || oldRemainingTime > 50)
                {
                    yield return CheckDespawnEnumerator();
                    yield return new WaitForSeconds(1);
                    oldRemainingTime = remainingTime;
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