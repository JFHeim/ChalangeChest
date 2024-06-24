using HarmonyLib;

namespace ChallengeChest.Patch;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyWrapSafe]
file static class FixChest
{
    [HarmonyPostfix, UsedImplicitly]
    private static void Postfix()
    {
        var challengeChest = ZNetScene.instance.GetPrefab("ChallengeChest");
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