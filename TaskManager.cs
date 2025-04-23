using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
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
        private readonly MessageBatcher _messageBatcher = new();
        private DateTime _lastProgressUpdate = DateTime.MinValue;

        // Constants for task generation and rewards
        private const int GOAL_LEVEL_SCALING = 2;
        private const double REWARD_MULTIPLIER = 2.0;
        private const int PROGRESS_UPDATE_INTERVAL_MS = 2000;

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
                // Get base enemy dictionary based on world state
                var enemies = Main.hardMode
                    ? NPCIdentifier
                        .HardmodeEnemies.Concat(NPCIdentifier.PreHardmodeEnemies)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    : NPCIdentifier.PreHardmodeEnemies;

                // Find all next bosses to unlock
                var nextBosses = TaskDefinitions
                    .BossLevelRequirements.Where(b => b.Value > _worldData.WorldLevel)
                    .OrderBy(b => b.Value)
                    .ToList();

                if (!nextBosses.Any())
                {
                    CreateFallbackTask();
                    return;
                }

                // Get the next level requirement
                var nextLevel = nextBosses.First().Value;

                // Get available groups that contain any of the next bosses or previous bosses
                var availableGroups = enemies
                    .Where(g =>
                        g.Value.Bosses.Any(b =>
                            // Include groups that have any boss from the next level
                            nextBosses.Any(nb => nb.Key == b && nb.Value == nextLevel)
                            ||
                            // Include groups for already unlocked bosses
                            (
                                TaskDefinitions.BossLevelRequirements.TryGetValue(
                                    b,
                                    out int reqLevel
                                )
                                && reqLevel <= _worldData.WorldLevel
                            )
                        )
                    )
                    .ToList();

                if (!availableGroups.Any())
                {
                    TShock.Log.Debug($"No enemy groups available for current progression");
                    CreateFallbackTask();
                    return;
                }

                // Select random group and create task
                var randomGroup = availableGroups[_random.Next(availableGroups.Count)];
                var availableNpcs = randomGroup.Value.NpcIds;
                var randomNpcId = availableNpcs[_random.Next(availableNpcs.Length)];

                // Get all bosses at the next level from this group
                var associatedBosses = randomGroup
                    .Value.Bosses.Where(b =>
                        nextBosses.Any(nb => nb.Key == b && nb.Value == nextLevel)
                    )
                    .ToList();

                var bossName = associatedBosses.Any()
                    ? string.Join("/", associatedBosses)
                    : nextBosses.First().Key.ToString();

                TShock.Log.Debug(
                    $"Creating task with NPC {randomNpcId} from group {randomGroup.Key} for boss(es) {bossName}"
                );
                CreateTask(randomNpcId, bossName, randomGroup.Key);
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error generating task: {ex}");
                CreateFallbackTask();
            }
        }

        private void CreateTask(int npcId, string bossName, string biome)
        {
            var goal = CalculateGoal(npcId);
            var reward = CalculateReward(npcId, goal, biome);

            _worldData.CurrentTask = new ActiveTask
            {
                TaskType = "KILL_ENEMIES",
                TargetMobId = npcId,
                AssociatedBoss = bossName,
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
                "Forest/Surface" => 1.2,
                "Desert" => 1.4,
                "Corruption" or "Crimson" => 1.8,
                "Snow" => 1.3,
                "Jungle" => 1.7,
                "Mushroom" => 1.3,
                "Ocean" => 1.3,
                "Caverns" => 1.5,
                "Floating Island" => 1.4,
                "Dungeon" => 2.0,
                "Underworld" => 2.1,
                // Hardmode biomes
                "Early Hardmode" => 2.2,
                "Mechanical" => 2.6,
                "Underground Jungle" => 2.9,
                "Hallow" => 2.3,
                "Temple" => 3.0,
                "Celestial" => 3.2,
                _ => 1.0, // Default/fallback value
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

            // Find active NPC instance with null checks and bounds validation
            NPC npc = null;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i] != null && !Main.npc[i].active && Main.npc[i].netID == npcId)
                {
                    npc = Main.npc[i];
                    break;
                }
            }

            // Verify this player is the actual killer with bounds checking
            if (
                npc != null
                && npc.lastInteraction >= 0
                && npc.lastInteraction < TShock.Players.Length
            )
            {
                if (npc.lastInteraction != killer.Index)
                {
                    TShock.Log.Debug(
                        $"Kill credit goes to player index {npc.lastInteraction}, not {killer.Index}"
                    );
                    var actualKiller = TShock.Players[npc.lastInteraction];
                    if (actualKiller != null && actualKiller.Active)
                    {
                        killer = actualKiller;
                    }
                }
            }

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

            // Show personal contribution with variant info
            var contribution = _currentTaskContributions[killer.Account.ID];
            var variantGroup = NPCVariants.GetVariantGroup(npcId);
            var killMessage =
                variantGroup != null
                    ? $"Your contribution ({variantGroup.Name}): {contribution.Kills} kills"
                    : $"Your contribution: {contribution.Kills} kills";

            killer.SendInfoMessage(
                $"{killMessage} ({contribution.Kills * 100.0f / _worldData.CurrentTask.Goal:F1}% of goal)"
            );

            // Batch progress broadcasts
            if (canBroadcast)
            {
                var now = DateTime.UtcNow;
                if ((now - _lastProgressUpdate).TotalMilliseconds >= PROGRESS_UPDATE_INTERVAL_MS)
                {
                    _messageBatcher.QueueMessage(
                        $"Task Progress: {_worldData.CurrentTask.Progress}/{_worldData.CurrentTask.Goal}",
                        Color.Yellow
                    );
                    _lastProgressUpdate = now;
                }
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
                    CreateTask(NPCID.BlueSlime, BossType.KingSlime.ToString(), "Surface");
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
                    CreateTask(NPCID.BlueSlime, BossType.KingSlime.ToString(), "Surface");
                    return;
                }

                // Select random NPC from the group
                var randomNpcId = taskGroup.Value.NpcIds[
                    _random.Next(taskGroup.Value.NpcIds.Length)
                ];

                TShock.Log.Debug(
                    $"Creating fallback task with NPC {randomNpcId} for boss {randomTask.Boss}"
                );
                CreateTask(randomNpcId, randomTask.Boss.ToString(), taskGroup.Key);
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error creating fallback task: {ex}");
                // Ultimate fallback - basic slime task
                CreateTask(NPCID.BlueSlime, BossType.KingSlime.ToString(), "Surface");
            }
        }

        private bool IsValidKillForTask(int npcId)
        {
            if (_worldData.CurrentTask == null)
            {
                TShock.Log.Debug("HandleNpcKill: No active task");
                return false;
            }

            var targetNpcId = _worldData.CurrentTask.TargetMobId;

            // Check if killed NPC is in same variant group as target
            if (NPCVariants.ShareSameVariantGroup(targetNpcId, npcId))
            {
                var group = NPCVariants.GetVariantGroup(targetNpcId);
                TShock.Log.Debug($"Variant group match found: {group?.Name}");
                return true;
            }

            // Fallback to exact match
            bool isExactMatch = targetNpcId == npcId;
            TShock.Log.Debug($"Direct ID match: {isExactMatch}");
            return isExactMatch;
        }
    }
}
