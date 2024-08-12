namespace ChallengeChest;

[Serializable]
public class EventData
{
    public Difficulty difficulty;
    public SimpleVector2 pos;
    public long despawnTime;
    public SimpleVector2 zone;

    public EventData(Difficulty difficulty, SimpleVector2 pos, SimpleVector2 zone, long despawnTime) : this()
    {
        this.difficulty = difficulty;
        this.pos = pos;
        this.zone = zone;
        this.despawnTime = despawnTime;
    }

    public EventData()
    {
    }

    public Heightmap.Biome GetBiome() => WorldGenerator.instance.GetBiome(pos.x, pos.y);
    public Vector2i GetZone() => new(zone.ToVector2());
    public Vector2i GetZoneTrue() => pos.ToVector2().ToV3().GetZone();

    public override string ToString()
    {
        return
            $"{nameof(difficulty)}: {difficulty}, " +
            $"{nameof(pos)}: {pos}, " +
            $"{nameof(despawnTime)}: {despawnTime}, " +
            $"{nameof(zone)}: {zone}, " +
            $"{nameof(GetZone)}: {GetZone()}, " +
            $"{nameof(GetZoneTrue)}: {GetZoneTrue()}";
    }
}