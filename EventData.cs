namespace ChallengeChest;

[Serializable]
public class EventData
{
    public SimpleVector3 Pos;
    public float time;
    public Difficulty difficulty;
    public float range;
    public string id;

    public EventData() { }

    public EventData(SimpleVector3 pos, Difficulty difficulty, float range) : this()
    {
        Pos = pos;
        this.difficulty = difficulty;
        this.range = range;
    }


    public static EventData Create(Vector3 pos)
    {
        var result = new EventData(pos, GenerateDifficulty(), EventRange.Value);
        result.id = Guid.NewGuid().ToString();
        result.time = EventTime.Value;
        result.time *= 60;
        result.time += Random.Range(-25, 25);
        result.time *= 60;
        return result;
    }

    private static Difficulty GenerateDifficulty() =>
        Random.Range(1, 101) switch
        {
            <= 50 => Difficulty.Normal,
            <= 70 => Difficulty.Okay,
            <= 80 => Difficulty.Good,
            <= 87 => Difficulty.Notgood,
            <= 92 => Difficulty.Hard,
            <= 97 => Difficulty.Impossible,
            _ => Difficulty.DeadlyPossible
        };

    public override string ToString() => $"[EventData] {difficulty} at {Pos.x};{Pos.y} time={time}";
}