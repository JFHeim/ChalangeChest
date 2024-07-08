namespace ChallengeChest.Patch;

public static class EventSetup
{
    public static void Init()
    {
        Debug("Initializing EventSetup...");
        EventData.Init();
        Debug("Done EventSetup init");
    }
}