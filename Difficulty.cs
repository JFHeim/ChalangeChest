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

    public static Difficulty GetDifficultyFromPrefab(this string prefab)
    {
        prefab = prefab.Replace("cc_Event_", "");
        Difficulty? d = Enum.Parse(typeof(Difficulty), prefab) is Difficulty difficulty ? difficulty : null;
        if (d is null) throw new Exception($"Could not parse difficulty from prefab {prefab}");
        return d.Value;
    }
}