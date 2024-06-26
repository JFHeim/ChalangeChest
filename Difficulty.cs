namespace ChallengeChest;

[NoReorder]
public enum Difficulty
{
    Normal = 0,
    Okay = 1,
    Good = 2,
    Notgood = 3,
    Hard = 4,
    Impossible = 5,
    DeadlyPossible = 6,
}

public static class Ext
{
    public static int GetStableHashCode(this Difficulty difficulty) => difficulty.ToString().GetStableHashCode();
    public static string Prefab(this Difficulty difficulty) => $"cc_Event_{difficulty.ToString()}";
    public static int PrefabHashCode(this Difficulty difficulty) => difficulty.Prefab().GetStableHashCode();

    public static Difficulty? GetDifficultyFromPrefab(this string prefab)
    {
        prefab = prefab.Replace("cc_Event_", "");
        Difficulty? d = Enum.Parse(typeof(Difficulty), prefab) is Difficulty difficulty ? difficulty : null;
        return d;
    }
}