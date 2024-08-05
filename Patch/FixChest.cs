using HarmonyLib;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyWrapSafe]
file static class FixChest
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix()
    {
        var challengeChest = ZNetScene.instance.GetPrefab("cc_SuccessChest_normal");
        var woodChest = ZNetScene.instance.GetPrefab("piece_chest_wood");
        if (!challengeChest) return;
        var container = challengeChest.GetComponent<Container>();
        var wearNTear = challengeChest.GetComponent<WearNTear>();
        var containerOrig = woodChest.GetComponent<Container>();
        var wearNTearOrig = woodChest.GetComponent<WearNTear>();

        container.m_bkg = containerOrig.m_bkg;
        container.m_openEffects = containerOrig.m_openEffects;
        container.m_closeEffects = containerOrig.m_closeEffects;
        wearNTear.m_health = wearNTearOrig.m_health;
        wearNTear.m_damages = wearNTearOrig.m_damages;
        wearNTear.m_destroyedEffect = wearNTearOrig.m_destroyedEffect;
        wearNTear.m_hitEffect = wearNTearOrig.m_hitEffect;
        wearNTear.m_switchEffect = wearNTearOrig.m_switchEffect;
    }
}

[HarmonyPatch(typeof(Container), nameof(Container.Awake)), HarmonyWrapSafe]
file static class ChestUnderground
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix(Container __instance)
    {
        if (ModEnabled.Value == false) return;
        if (__instance.GetPrefabName() != "cc_SuccessChest_normal") return;
        __instance.StartCoroutine(CoroutineLogic(__instance));
    }

    private static IEnumerator CoroutineLogic(Container container)
    {
        while (true)
        {
            Logic(container);

            yield return new WaitForSeconds(2);
        }
        // ReSharper disable once IteratorNeverReturns
    }

    private static void Logic(Container container)
    {
        Debug($"ChestUnderground 0");
        if (container.m_nview?.IsOwner() != true) return;
        Debug($"ChestUnderground 1");
        var groundHeight = ZoneSystem.instance.GetGroundHeight(container.transform.position);
        Debug($"ChestUnderground 2");
        if (container.transform.position.y >= groundHeight - 1.0)
            return;
        var position = container.transform.position with
        {
            y = groundHeight + 0.1f
        };
        container.transform.position = position;
        Debug($"ChestUnderground 3");
    }
}