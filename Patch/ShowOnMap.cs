using HarmonyLib;

namespace ChalangeChest.Patch;

[HarmonyPatch, HarmonyWrapSafe]
public class ShowOnMap
{
    public static bool needsUpdate = false;
    private static List<(Minimap.PinData, Minimap.PinData)> eventPins = new();

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateEventPin))]
    [HarmonyPostfix]
    private static void UpdatePins(Minimap __instance)
    {
        if (needsUpdate == false) return;
        needsUpdate = false;

        foreach (var (pin, areaPin) in eventPins)
        {
            __instance.RemovePin(pin);
            __instance.RemovePin(areaPin);
        }

        eventPins.Clear();

        foreach (var eventData in currentEvents)
        {
            var pos = eventData.pos;
            var areaPin = __instance.AddPin(pos, Minimap.PinType.EventArea, "", false, false);
            var pin = __instance.AddPin(pos, Minimap.PinType.RandomEvent, "", false, false);
            eventPins.Add((pin, areaPin));
        }
    }
}