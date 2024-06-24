namespace ChallengeChest;

public enum Difficulty
{
    Normal,
    Okay,
    Good,
    Notgood,
    Hard,
    Impossible,
    DeadlyPossible,
}

public static class Ext
{
    public static int GetStableHashCode(this Difficulty difficulty) => difficulty.ToString().GetStableHashCode();
    public static string Prefab(this Difficulty difficulty) => $"cc_Event_{difficulty.ToString()}";
    public static int PrefabHashCode(this Difficulty difficulty) => difficulty.Prefab().GetStableHashCode();
}