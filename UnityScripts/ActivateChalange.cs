namespace ChallengeChest.UnityScripts;

public class ActivateChalange : MonoBehaviour, Hoverable, Interactable
{
    public string GetHoverText() => $"{ModName}_ActivateChalange".Localize();

    public string GetHoverName() => $"{ModName}_ActivateChalange".Localize();

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (!hold) return false;
        // TODO: Add activation logic
        return true;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
}