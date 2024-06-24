namespace ChallengeChest.Patch;

public static class EventSetup
{
    private static readonly List<EventData> Events = [];
    
    public static void Init()
    {
        Debug($"Initializing EventSetup...");
        Events.Add(new EventData("cc_Event_Normal"));

        EventSpawn.Icons[Difficulty.Normal] = RegisterPrefabs.Sprite("cc_IconNormal");
        
        Debug($"Done EventSetup init");
    }
}