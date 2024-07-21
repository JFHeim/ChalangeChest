using HarmonyLib;
using static ChallengeChest.Patch.ShowMobsOnMap;
using PinData = Minimap.PinData;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(Minimap), nameof(Minimap.Awake))]
[HarmonyWrapSafe]
file static class ShowMobsOnMap
{
    public static List<Container> chests = [];
    private static List<PinData> _addedPinDatas = [];
    private static List<PinData> _oldPinDatas = [];
    static Sprite icon;

    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(Minimap __instance)
    {
        __instance.StartCoroutine(CoroutineLogic());
    }

    private static IEnumerator CoroutineLogic()
    {
        while (true)
        {
            try
            {
                Logic();
            }
            catch (Exception e)
            {
                DebugError(e);
            }

            yield return new WaitForSeconds(2);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private static void Logic()
    {
        if (!ZNet.instance) return;
        if (!ZNetScene.instance) return;
        if (!ZoneSystem.instance) return;
        var map = Minimap.instance;
        if (!map) return;
        if (!icon) icon = RegisterPrefabs.Sprite("cc_IconChest");

        foreach (var mob in Character.s_characters)
        {
            var vector3 = mob?.m_nview?.GetZDO()?.GetVec3("ChallengeChestPos", Vector3.zero) ?? Vector3.zero;
            if (vector3 == Vector3.zero) continue;
            var mobPin = new PinData
            {
                m_type = Minimap.PinType.Boss,
                m_pos = mob!.transform.position,
                m_icon = icon,
                m_animate = false,
                m_save = false,
                m_name = ""
            };
            _addedPinDatas.Add(mobPin);
        }

        foreach (var chest in chests)
        {
            if (!chest) continue;
            var mobPin = new PinData
            {
                m_type = Minimap.PinType.Boss,
                m_pos = chest.transform.position,
                m_icon = icon,
                m_animate = false,
                m_save = false,
                m_doubleSize = true,
                m_name = "Chest"
            };
            _addedPinDatas.Add(mobPin);
        }

        foreach (var pinData in _addedPinDatas) map.m_pins.Add(pinData);
        foreach (var pin in _oldPinDatas)
        {
            if (pin.m_uiElement)
            {
                Destroy(pin.m_uiElement.gameObject);
                pin.m_uiElement = null;
            }

            if (pin.m_NamePinData != null)
            {
                Destroy(pin.m_NamePinData.PinNameGameObject);
                pin.m_NamePinData.PinNameGameObject = null;
            }

            map.m_pins.Remove(pin);
        }

        _oldPinDatas.Clear();
        _oldPinDatas = [.._addedPinDatas];
        if (_addedPinDatas.Count > 0) map.m_pinUpdateRequired = true;
        _addedPinDatas.Clear();
    }   
}

[HarmonyPatch(typeof(Container))]
[HarmonyWrapSafe]
file static class RegisterChest
{
    [HarmonyPatch(nameof(Container.Awake))]
    [HarmonyPostfix, UsedImplicitly]
    private static void PostfixAwake(Container __instance)
    {
        if (__instance.GetPrefabName() != "cc_SuccessChest_normal") return;
        chests.Add(__instance);
    }

    [HarmonyPatch(nameof(Container.OnDestroyed))]
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(Container __instance)
    {
        if (__instance.GetPrefabName() != "cc_SuccessChest_normal") return;
        chests.Remove(__instance);
    }
}