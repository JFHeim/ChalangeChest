using ChalangeChest.Compatibility.ESP;
using fastJSON;
using HarmonyLib;

namespace ChalangeChest.Patch;

[HarmonyPatch, HarmonyWrapSafe]
public class UpdateEventLifecycle
{
    [HarmonyPatch(typeof(RandEventSystem), nameof(RandEventSystem.FixedUpdate))]
    [HarmonyPostfix]
    private static void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (!ZNet.instance.IsServer()) return;

        foreach (var eventData in currentEvents)
        {
            eventData.time -= dt;
            if (eventData.time <= 0)
            {
                currentEvents.Remove(eventData);
                ShowOnMap.needsUpdate = true;
                break;
            }
        }

        if (espApi.IsLoaded())
        {
            foreach (var eventData in currentEvents)
            {
                
            }
        }
    }
}