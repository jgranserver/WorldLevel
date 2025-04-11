namespace WorldLevel.Models
{
    public record MobInfo(string Name, double DifficultyMultiplier);

    public class BossTaskGroup
    {
        public int RequiredWorldLevel { get; init; }
        public Dictionary<int, MobInfo> RelatedMobs { get; init; }

        public BossTaskGroup(int requiredLevel)
        {
            RequiredWorldLevel = requiredLevel;
            RelatedMobs = new Dictionary<int, MobInfo>();
        }
    }
}
