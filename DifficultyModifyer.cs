namespace ChalangeChest;

[Serializable]
public class DifficultyModifyer
{
    public List<MonsterMod> monsters = [];
    public List<string> bannedMonsters = [];

    public class MonsterMod
    {
        public string name;
        public float? spawnChance;
        public float? countMin;
        public float? countMax;
        public float? starChance;
        public float? star2Chance;
        public MonsterMod() { }

        public MonsterMod(string name, float? spawnCh = null, float? min = null, float? max = null,
            float? starCh = null, float? star2Ch = null)
        {
            this.name = name;
            this.spawnChance = spawnCh;
            this.countMin = min;
            this.countMax = max;
            this.starChance = starCh;
            this.star2Chance = star2Ch;
        }
    }
}