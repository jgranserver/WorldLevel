using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using TShockAPI;
using WorldLevel.Models;
using WorldLevel.Services;

namespace WorldLevel
{
    public class TaskManager
    {
        private readonly WorldData _worldData;
        private readonly Random _random = new();
        private readonly BankService _bankService;
        private readonly Dictionary<int, TaskContribution> _currentTaskContributions;
        private readonly NPCRarityService _npcRarityService;

        // Constants for task generation and rewards
        private const int GOAL_LEVEL_SCALING = 2;
        private const double REWARD_MULTIPLIER = 2.0;

        public TaskManager(WorldData worldData)
        {
            _worldData = worldData;
            _bankService = new BankService();
            _currentTaskContributions = new Dictionary<int, TaskContribution>();
            _npcRarityService = new NPCRarityService();
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

                // Find next boss to unlock based on world level
                var nextBoss = TaskDefinitions
                    .BossLevelRequirements.Where(b => b.Value > _worldData.WorldLevel)
                    .OrderBy(b => b.Value)
                    .FirstOrDefault();

                if (nextBoss.Equals(default(KeyValuePair<BossType, int>)))
                {
                    TShock.Log.Debug("No more bosses to unlock, using fallback task");
                    CreateFallbackTask();
                    return;
                }

                TShock.Log.Debug(
                    $"Generating task for next boss: {nextBoss.Key} (Level {nextBoss.Value})"
                );

                // Get available biome groups that are appropriate for the next boss
                var availableGroups = enemies
                    .Where(g => g.Value.Bosses.Contains(nextBoss.Key))
                    .ToList();

                if (!availableGroups.Any())
                {
                    TShock.Log.Debug($"No enemy groups available for boss {nextBoss.Key}");
                    CreateFallbackTask();
                    return;
                }

                // Select random group and NPC
                var randomGroup = availableGroups[_random.Next(availableGroups.Count)];
                TShock.Log.Debug($"Selected biome group: {randomGroup.Key}");

                var availableNpcs = randomGroup.Value.NpcIds;
                var randomNpcId = availableNpcs[_random.Next(availableNpcs.Length)];

                TShock.Log.Debug($"Creating task with NPC {randomNpcId} for boss {nextBoss.Key}");
                CreateTask(randomNpcId, nextBoss.Key, randomGroup.Key);
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error generating task: {ex}");
                CreateFallbackTask();
            }
        }

        private void CreateTask(int npcId, BossType bossType, string biome)
        {
            var goal = CalculateGoal(npcId);
            var reward = CalculateReward(npcId, goal, biome);

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

        private int CalculateGoal(int npcId)
        {
            var rarity = _npcRarityService.GetNPCRarity(npcId);
            var baseRequirement = _npcRarityService.GetRequiredKills(rarity);

            // Scale with world level
            return baseRequirement + (_worldData.WorldLevel * GOAL_LEVEL_SCALING);
        }

        private int CalculateReward(int npcId, int kills, string biome)
        {
            var baseReward = kills * REWARD_MULTIPLIER;
            var biomeDifficulty = GetBiomeDifficulty(biome);
            var xpMultiplier = _npcRarityService.GetXPMultiplier(npcId);

            return (int)(baseReward * biomeDifficulty * xpMultiplier);
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

        private async void CheckTaskCompletion()
        {
            if (_worldData.CurrentTask?.Progress >= _worldData.CurrentTask?.Goal)
            {
                await CompleteTask();
            }
        }

        public async void HandleNpcKill(int npcId, TSPlayer killer, bool canBroadcast = true)
        {
            if (_worldData.CurrentTask == null || !IsValidKillForTask(npcId))
                return;

            // Track contribution
            if (!_currentTaskContributions.ContainsKey(killer.Account.ID))
            {
                _currentTaskContributions[killer.Account.ID] = new TaskContribution(
                    killer.Account.ID,
                    killer.Name
                );
            }

            _currentTaskContributions[killer.Account.ID].Kills++;
            _worldData.CurrentTask.Progress++;

            // Show personal contribution
            var contribution = _currentTaskContributions[killer.Account.ID];
            killer.SendInfoMessage(
                $"Your contribution: {contribution.Kills} kills ({contribution.Kills * 100.0f / _worldData.CurrentTask.Goal:F1}% of goal)"
            );

            // Only broadcast progress if allowed
            if (canBroadcast)
            {
                TaskBroadcaster.BroadcastProgress(_worldData.CurrentTask, _worldData);
            }

            if (_worldData.CurrentTask.Progress >= _worldData.CurrentTask.Goal)
            {
                await CompleteTask();
            }
        }

        private async Task CompleteTask()
        {
            if (_worldData.CurrentTask == null)
                return;

            // Calculate base reward based on world level
            int baseReward = 100000 + (_worldData.WorldLevel * 500);

            // Distribute rewards based on contributions
            await _bankService.DistributeTaskRewards(_currentTaskContributions, baseReward);

            // Broadcast completion messages with contribution info
            var topContributor = _currentTaskContributions
                .OrderByDescending(c => c.Value.Kills)
                .FirstOrDefault();

            if (topContributor.Value != null)
            {
                TShock.Utils.Broadcast(
                    $"Task Complete! Top contributor: {topContributor.Value.PlayerName} with {topContributor.Value.Kills} kills!",
                    Microsoft.Xna.Framework.Color.LightGreen
                );
            }

            // Clear contributions for next task
            _currentTaskContributions.Clear();

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
            try
            {
                // Get level 0 tasks
                var level0Tasks = TaskDefinitions
                    .BossTasks.Where(bt => bt.Task.RequiredLevel == 0)
                    .ToList();

                if (!level0Tasks.Any())
                {
                    TShock.Log.Error("No level 0 tasks found, using basic slime task");
                    CreateTask(NPCID.BlueSlime, BossType.KingSlime, "Surface");
                    return;
                }

                // Select random level 0 task
                var randomTask = level0Tasks[_random.Next(level0Tasks.Count)];

                // Get appropriate enemy group for the task
                var enemies = NPCIdentifier.PreHardmodeEnemies;
                var taskGroup = enemies.FirstOrDefault(g =>
                    g.Value.Bosses.Contains(randomTask.Boss)
                );

                if (
                    taskGroup.Equals(
                        default(KeyValuePair<string, (int[] NpcIds, HashSet<BossType> Bosses)>)
                    )
                )
                {
                    TShock.Log.Error(
                        $"No enemy group found for boss {randomTask.Boss}, using basic enemies task"
                    );
                    CreateTask(NPCID.BlueSlime, BossType.KingSlime, "Surface");
                    return;
                }

                // Select random NPC from the group
                var randomNpcId = taskGroup.Value.NpcIds[
                    _random.Next(taskGroup.Value.NpcIds.Length)
                ];

                TShock.Log.Debug(
                    $"Creating fallback task with NPC {randomNpcId} for boss {randomTask.Boss}"
                );
                CreateTask(randomNpcId, randomTask.Boss, taskGroup.Key);
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error creating fallback task: {ex}");
                // Ultimate fallback - basic slime task
                CreateTask(NPCID.BlueSlime, BossType.KingSlime, "Surface");
            }
        }

        private bool IsValidKillForTask(int npcId)
        {
            // Add debug logging for task validation
            if (_worldData.CurrentTask == null)
            {
                TShock.Log.Debug($"HandleNpcKill: No active task");
                return false;
            }

            // Log current task details for debugging
            TShock.Log.Debug(
                $"HandleNpcKill: Current Task - Target: {_worldData.CurrentTask.TargetMobId}, Killed: {npcId}"
            );

            // Handle both positive and negative NPC IDs
            bool isValidKill = false;
            if (_worldData.CurrentTask.TargetMobId < 0)
            {
                // For negative IDs (special events), check exact match
                isValidKill = _worldData.CurrentTask.TargetMobId == npcId;
                TShock.Log.Debug(
                    $"HandleNpcKill: Checking special event NPC - Match: {isValidKill}"
                );
            }
            else if (npcId > 0)
            {
                // For positive IDs (regular NPCs), check normal conditions
                isValidKill = _worldData.CurrentTask.TargetMobId == npcId;

                if (!isValidKill)
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
                }
            }

            if (!isValidKill)
            {
                TShock.Log.Debug($"HandleNpcKill: Invalid kill - NPC ID: {npcId}");
                return false;
            }

            // Valid kill for current task
            TShock.Log.Debug(
                $"HandleNpcKill: Valid kill for task - Progress: {_worldData.CurrentTask.Progress + 1}/{_worldData.CurrentTask.Goal}"
            );

            return true;
        }
    }
}
