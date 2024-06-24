namespace ChallengeChest;

[Serializable]
public class DifficultyModifyer
{
    public List<MonsterMod> Monsters = [];
    public List<string> bannedMonsters = [];

    public class MonsterMod
    {
        public string Name;
        public float? SpawnChance;
        public float? CountMin;
        public float? CountMax;
        public float? StarChance;
        public float? Star2Chance;
        public MonsterMod() { }

        public MonsterMod(string name, float? spawnCh = null, float? min = null, float? max = null,
            float? starCh = null, float? star2Ch = null)
        {
            Name = name;
            SpawnChance = spawnCh;
            CountMin = min;
            CountMax = max;
            StarChance = starCh;
            Star2Chance = star2Ch;
        }
    }
}