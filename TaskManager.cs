using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using TShockAPI;
using WorldLevel.Models;

namespace WorldLevel
{
    public class TaskManager
    {
        private readonly WorldData _worldData;
        private readonly Random _random = new();

        // Constants for task generation and rewards
        private const int BASE_GOAL = 100;
        private const int GOAL_LEVEL_SCALING = 2;
        private const double REWARD_MULTIPLIER = 2.0;
        private const int MIN_MOBS_REQUIRED = 3;

        public TaskManager(WorldData worldData)
        {
            _worldData = worldData;
            // Initialize required XP for first level if not set
            if (_worldData.RequiredXP == 0)
            {
                _worldData.SetRequiredXP(TaskDefinitions.GetRequiredXPForLevel(1));
            }
        }

        public void Update()
        {
            if (_worldData.CurrentTask == null)
            {
                GenerateNewTask();
                return;
            }

            CheckTaskCompletion();

            if (_worldData.CurrentTask?.Progress % 5 == 0) // Every 5 kills
            {
                TaskBroadcaster.BroadcastProgress(_worldData.CurrentTask, _worldData);
            }
        }

        private void GenerateNewTask()
        {
            try
            {
                // Get appropriate enemy dictionary
                var enemies = Main.hardMode
                    ? NPCIdentifier.HardmodeEnemies
                    : NPCIdentifier.PreHardmodeEnemies;

                // Log available enemy groups for debugging
                foreach (var group in enemies)
                {
                    TShock.Log.Debug(
                        $"Available enemy group: {group.Key} with {group.Value.NpcIds.Length} NPCs"
                    );
                }

                // Get task group for current level
                var taskGroup = TaskDefinitions.GetAppropriateTaskGroup(
                    _worldData.WorldLevel,
                    Main.hardMode
                );
                if (taskGroup == null)
                {
                    TShock.Log.Debug($"No task group found for level {_worldData.WorldLevel}");
                    CreateFallbackTask();
                    return;
                }

                TShock.Log.Debug($"Found task group for level {taskGroup.RequiredLevel}");

                // Get available biome groups that match the task level
                var availableGroups = enemies
                    .Where(g =>
                        g.Value.Bosses.Any(b =>
                            TaskDefinitions.GetRequiredLevelForBoss(b) <= _worldData.WorldLevel
                        )
                    )
                    .ToList();

                if (!availableGroups.Any())
                {
                    TShock.Log.Debug(
                        $"No enemy groups available for level {_worldData.WorldLevel}"
                    );
                    CreateFallbackTask();
                    return;
                }

                // Select random group and NPC
                var randomGroup = availableGroups[_random.Next(availableGroups.Count)];
                TShock.Log.Debug($"Selected biome group: {randomGroup.Key}");

                var availableNpcs = randomGroup.Value.NpcIds;
                var randomNpcId = availableNpcs[_random.Next(availableNpcs.Length)];
                var associatedBoss = randomGroup.Value.Bosses.First();

                TShock.Log.Debug($"Creating task with NPC {randomNpcId} for boss {associatedBoss}");
                CreateTask(randomNpcId, associatedBoss, randomGroup.Key);
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error generating task: {ex}");
                CreateFallbackTask();
            }
        }

        private void CreateTask(int npcId, BossType bossType, string biome)
        {
            var goal = CalculateGoal();
            var reward = CalculateReward(goal, GetBiomeDifficulty(biome));

            _worldData.CurrentTask = new ActiveTask
            {
                TaskType = "KILL_ENEMIES",
                TargetMobId = npcId,
                AssociatedBoss = bossType.ToString(),
                Goal = goal,
                Progress = 0,
                RewardXP = reward,
            };

            TaskBroadcaster.AnnounceNewTask(_worldData.CurrentTask, _worldData, biome);
        }

        private int CalculateGoal() =>
            Math.Max(MIN_MOBS_REQUIRED, BASE_GOAL + (_worldData.WorldLevel * GOAL_LEVEL_SCALING));

        private int CalculateReward(int goal, double difficultyMultiplier)
        {
            // Scale rewards based on current level and make them more gradual
            var baseReward = goal * REWARD_MULTIPLIER;
            var levelMultiplier = Math.Max(1.0, _worldData.WorldLevel * 0.5);
            return (int)(baseReward * levelMultiplier * difficultyMultiplier);
        }

        private double GetBiomeDifficulty(string biome) =>
            biome switch
            {
                "Underground" => 1.2,
                "Corruption" or "Crimson" => 1.3,
                "Dungeon" => 1.4,
                "Underworld" => 1.5,
                "Hallow" => 1.6,
                "Temple" => 1.7,
                "Space" => 1.8,
                _ => 1.0,
            };

        private void CheckTaskCompletion()
        {
            if (_worldData.CurrentTask?.Progress >= _worldData.CurrentTask?.Goal)
            {
                CompleteTask();
            }
        }

        public void HandleNpcKill(int npcId, bool canBroadcast)
        {
            // Add debug logging for task validation
            if (_worldData.CurrentTask == null)
            {
                TShock.Log.Debug($"HandleNpcKill: No active task");
                return;
            }

            // Log current task details for debugging
            TShock.Log.Debug(
                $"HandleNpcKill: Current Task - Target: {_worldData.CurrentTask.TargetMobId}, Killed: {npcId}"
            );

            if (_worldData.CurrentTask.TargetMobId != npcId)
            {
                // Check if NPC belongs to current task's biome group
                var enemies = Main.hardMode
                    ? NPCIdentifier.HardmodeEnemies
                    : NPCIdentifier.PreHardmodeEnemies;

                var biomeGroup = enemies.FirstOrDefault(g => g.Value.NpcIds.Contains(npcId));
                if (!string.IsNullOrEmpty(biomeGroup.Key))
                {
                    TShock.Log.Debug($"HandleNpcKill: NPC belongs to biome {biomeGroup.Key}");
                }
                return;
            }

            // Valid kill for current task
            TShock.Log.Debug(
                $"HandleNpcKill: Valid kill for task - Progress: {_worldData.CurrentTask.Progress + 1}/{_worldData.CurrentTask.Goal}"
            );

            _worldData.CurrentTask.Progress++;

            // Only try to broadcast if allowed
            if (canBroadcast)
            {
                TaskBroadcaster.BroadcastProgress(_worldData.CurrentTask, _worldData);
            }

            // Check for task completion
            if (_worldData.CurrentTask.Progress >= _worldData.CurrentTask.Goal)
            {
                TShock.Log.Debug("HandleNpcKill: Task complete, calling CompleteTask()");
                CompleteTask();
            }
        }

        private void CompleteTask()
        {
            if (_worldData.CurrentTask == null)
                return;

            var oldLevel = _worldData.WorldLevel;
            _worldData.CurrentXP += _worldData.CurrentTask.RewardXP;

            // Check for level up
            while (_worldData.CurrentXP >= _worldData.RequiredXP)
            {
                _worldData.WorldLevel++;
                _worldData.CurrentXP -= _worldData.RequiredXP;
                // Set required XP for next level
                _worldData.SetRequiredXP(
                    TaskDefinitions.GetRequiredXPForLevel(_worldData.WorldLevel)
                );

                TShock.Log.Debug($"Level up: {oldLevel} -> {_worldData.WorldLevel}");
                TShock.Log.Debug($"New XP requirement: {_worldData.RequiredXP}");
            }

            // Announce completion and any level up
            TaskBroadcaster.AnnounceTaskCompletion(
                _worldData.CurrentTask,
                _worldData.WorldLevel > oldLevel ? _worldData.WorldLevel : 0
            );

            _worldData.CurrentTask = null;
        }

        private void CreateFallbackTask()
        {
            CreateTask(NPCID.BlueSlime, BossType.KingSlime, "Surface");
        }
    }
}
