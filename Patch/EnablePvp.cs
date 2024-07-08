using HarmonyLib;
using UnityEngine.SceneManagement;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(Player), nameof(Player.Awake)), HarmonyWrapSafe]
file static class EnablePvp
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(Player __instance) =>
        __instance.StartCoroutine(Logic(__instance));

    private static IEnumerator Logic(Player __instance)
    {
        while (true)
        {
            yield return new WaitForSeconds(3);
            var pl = m_localPlayer;
            if (__instance != pl) continue;
            if (!Minimap.instance) continue;
            if (!ForcePvp.Value) continue;
            if (SceneManager.GetActiveScene().name != "main") continue;
            if (pl.IsPVPEnabled()) continue;
            var pinPoss = Minimap.instance.m_pins
                .Where(p => EventData.Icons.Values.Contains(p.m_icon))
                .Select(x => x.m_pos)
                .ToList();

            foreach (var pos in pinPoss)
            {
                if (pl.IsPVPEnabled()) continue;
                var distance = pl.transform.DistanceXZ(pos);
                if (distance > EventData.Range + 20) continue;
                pl.SetPVP(true);
            }
        }
        // ReSharper disable once IteratorNeverReturns
    }
}