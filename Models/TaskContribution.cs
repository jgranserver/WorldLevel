namespace WorldLevel.Models
{
    public class TaskContribution
    {
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public int Kills { get; set; }
        public double ContributionPercentage { get; set; }
        public int RewardAmount { get; set; }

        public TaskContribution(int playerId, string playerName)
        {
            PlayerID = playerId;
            PlayerName = playerName;
            Kills = 0;
            ContributionPercentage = 0;
            RewardAmount = 0;
        }
    }
}
