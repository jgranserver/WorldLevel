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
            var location = GetBiomeDescription(biome);

            // Find next boss to unlock
            var nextBoss = TaskDefinitions
                .BossLevelRequirements.Where(b => b.Value > worldData.WorldLevel)
                .OrderBy(b => b.Value)
                .FirstOrDefault();

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

            // Next boss announcement
            if (!nextBoss.Equals(default(KeyValuePair<BossType, int>)))
            {
                TSPlayer.All.SendMessage(
                    $"Complete tasks to reach Level {nextBoss.Value} and unlock {nextBoss.Key}!",
                    Color.LightBlue
                );
            }
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
                // Boss progression biomes
                "Boss1" => "Pre-Eye territories", // King Slime & Eye of Cthulhu
                "Boss2" => "Evil biome depths", // EoW/BoC
                "Boss3" => "Queen's domain", // Queen Bee & Deerclops
                "Boss4" => "Dungeon outskirts", // Skeletron
                "Boss5" => "Underworld border", // Wall of Flesh
                "Boss6" => "Early hardmode realm", // Queen Slime
                "Boss7" => "Mechanical wasteland", // Mechanical Bosses
                "Boss8" => "Post-mechanical territories", // Late game bosses
                _ => biome,
            };
    }

    public class MessageBatcher
    {
        private readonly Queue<(string Message, Color Color)> _messageQueue = new();
        private readonly Timer _batchTimer;
        private readonly object _lockObject = new();
        private const int BATCH_INTERVAL_MS = 2000; // 2 seconds

        public MessageBatcher()
        {
            _batchTimer = new Timer(ProcessBatch, null, BATCH_INTERVAL_MS, BATCH_INTERVAL_MS);
        }

        public void QueueMessage(string message, Color color)
        {
            lock (_lockObject)
            {
                _messageQueue.Enqueue((message, color));
            }
        }

        private void ProcessBatch(object state)
        {
            List<(string Message, Color Color)> messagesToSend;

            lock (_lockObject)
            {
                if (!_messageQueue.Any())
                    return;

                messagesToSend = _messageQueue.ToList();
                _messageQueue.Clear();
            }

            // Send all queued messages at once
            foreach (var (message, color) in messagesToSend)
            {
                TShock.Utils.Broadcast(message, color);
            }
        }
    }
}
