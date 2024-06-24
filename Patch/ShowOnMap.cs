using HarmonyLib;

namespace ChallengeChest.Patch;

[HarmonyPatch, HarmonyWrapSafe]
public class ShowOnMap
{
    public static bool needsUpdate;
    private static List<(Minimap.PinData, Minimap.PinData)> _eventPins = [];

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateEventPin))]
    [HarmonyPostfix]
    private static void UpdatePins(Minimap __instance)
    {
        if (needsUpdate == false) return;
        needsUpdate = false;

        foreach (var (pin, areaPin) in _eventPins)
        {
            __instance.RemovePin(pin);
            __instance.RemovePin(areaPin);
        }

        _eventPins.Clear();

        foreach (var eventData in currentEvents)
        {
            var pos = eventData.Pos;
            var areaPin = __instance.AddPin(pos, Minimap.PinType.EventArea, "", false, false);
            var pin = __instance.AddPin(pos, Minimap.PinType.RandomEvent, "", false, false);
            _eventPins.Add((pin, areaPin));
        }
    }
}