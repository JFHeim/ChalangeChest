namespace ChallengeChest.Patch;

public static class EventSetup
{
    public static void Init()
    {
        Debug("Initializing EventSetup...");
        new EventData("cc_Event_Normal", "cc_IconNormal");

        Debug("Done EventSetup init");
    }
}