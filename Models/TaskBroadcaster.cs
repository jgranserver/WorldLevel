using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;

namespace WorldLevel.Models
{
    public static class TaskBroadcaster
    {
        // Track last broadcast time
        private static DateTime _lastProgressBroadcast = DateTime.MinValue;
        private const int BROADCAST_COOLDOWN_SECONDS = 30;

        public static void AnnounceNewTask(ActiveTask task, WorldData worldData, string biome)
        {
            var npcName = Lang.GetNPCNameValue(task.TargetMobId);
            var bossType = Enum.Parse<BossType>(task.AssociatedBoss);
            var location = GetBiomeDescription(biome);

            // Main task announcement
            TSPlayer.All.SendMessage(
                $"[World Level {worldData.WorldLevel}] New Task Available!",
                Color.LightGreen
            );

            // Task details
            TSPlayer.All.SendMessage($"Hunt {task.Goal} {npcName} in the {location}", Color.White);

            // Progress and reward
            TSPlayer.All.SendMessage(
                $"Progress: 0/{task.Goal} - Reward: {task.RewardXP} XP",
                Color.Yellow
            );

            // Boss association
            TSPlayer.All.SendMessage($"This will help prepare for {bossType}!", Color.LightBlue);
        }

        public static void BroadcastProgress(ActiveTask task, WorldData worldData)
        {
            // Check if enough time has passed since last broadcast
            if ((DateTime.Now - _lastProgressBroadcast).TotalSeconds < BROADCAST_COOLDOWN_SECONDS)
            {
                return;
            }

            var progressPercent = (task.Progress * 100) / task.Goal;

            var npcName = Lang.GetNPCNameValue(task.TargetMobId);
            TSPlayer.All.SendMessage(
                $"[Task Progress] {task.Progress}/{task.Goal} {npcName} ({progressPercent}%)",
                Color.Yellow
            );

            _lastProgressBroadcast = DateTime.Now;
        }

        public static void AnnounceTaskCompletion(ActiveTask task, int worldLevel)
        {
            TSPlayer.All.SendMessage("=================================", Color.Gold);

            TSPlayer.All.SendMessage(
                $"Task Complete! Earned {task.RewardXP} XP!",
                Color.LightGreen
            );

            if (worldLevel > 0)
            {
                TSPlayer.All.SendMessage($"World Level increased to {worldLevel}!", Color.Pink);
            }

            TSPlayer.All.SendMessage("=================================", Color.Gold);
        }

        private static string GetBiomeDescription(string biome) =>
            biome switch
            {
                "Forest/Surface" => "Surface and Forest areas",
                "Underground" => "Underground caves",
                "Corruption" => "corrupted lands",
                "Crimson" => "crimson territory",
                "Desert" => "desert wasteland",
                "Jungle" => "dangerous jungle",
                "Snow" => "frozen tundra",
                "Dungeon" => "ancient dungeon",
                "Underworld" => "depths of hell",
                "Mechanical" => "mechanical wasteland",
                "Hallow" => "holy lands",
                "Underground Jungle" => "underground jungle",
                "Temple" => "Lihzahrd Temple",
                "Ocean" => "deep ocean",
                "Space" => "outer space",
                _ => biome,
            };
    }
}
