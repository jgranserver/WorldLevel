using System.Collections.Immutable;
using Terraria.ID;

namespace WorldLevel.Models
{
    public record TaskGroup(
        int RequiredLevel,
        string Description,
        string MinionDescription,
        int BaseXPReward
    );

    public static class TaskDefinitions
    {
        // Adjusted XP constants for proper level 0 progression
        private const int BASE_XP = 10240; // Much lower base XP for level 0
        private const double LEVEL_MULTIPLIER = 3.8; // Gentler scaling

        public static readonly List<(BossType Boss, TaskGroup Task)> BossTasks = new()
        {
            // Tutorial/Beginner Tasks (Level 0)
            (
                BossType.KingSlime,
                new TaskGroup(0, "Welcome to Terraria", "Defeat surface enemies", 5)
            ),
            (BossType.KingSlime, new TaskGroup(0, "Night Survival", "Defeat night enemies", 5)),
            (BossType.EyeOfCthulhu, new TaskGroup(0, "Basic Training", "Defeat flying enemies", 5)),
            (BossType.EyeOfCthulhu, new TaskGroup(0, "Safety Mining", "Defeat cave enemies", 5)),
            // Pre-Hardmode Boss Tasks (Levels 1-5)
            (BossType.KingSlime, new TaskGroup(1, "Surface Hunt", "Defeat slime enemies", 10)),
            (BossType.KingSlime, new TaskGroup(1, "Royal Hunt", "Defeat rare enemies", 12)),
            (BossType.EyeOfCthulhu, new TaskGroup(1, "Night Watch", "Defeat flying enemies", 15)),
            (BossType.EyeOfCthulhu, new TaskGroup(1, "Eye Hunter", "Defeat demon eye enemies", 16)),
            (
                BossType.EaterOfWorlds,
                new TaskGroup(2, "Corruption Hunt", "Defeat corruption enemies", 20)
            ),
            (BossType.EaterOfWorlds, new TaskGroup(2, "Shadow Hunt", "Defeat eater enemies", 22)),
            (
                BossType.BrainOfCthulhu,
                new TaskGroup(2, "Crimson Hunt", "Defeat crimson enemies", 20)
            ),
            (
                BossType.BrainOfCthulhu,
                new TaskGroup(2, "Blood Hunt", "Defeat blood moon enemies", 22)
            ),
            (BossType.Deerclops, new TaskGroup(3, "Snow Hunt", "Defeat snow biome enemies", 25)),
            (BossType.Deerclops, new TaskGroup(3, "Ice Hunt", "Defeat ice enemies", 27)),
            (BossType.QueenBee, new TaskGroup(3, "Jungle Hunt", "Defeat jungle enemies", 25)),
            (BossType.QueenBee, new TaskGroup(3, "Hive Hunt", "Defeat hive enemies", 27)),
            (BossType.Skeletron, new TaskGroup(4, "Dungeon Hunt", "Defeat dungeon enemies", 30)),
            (BossType.Skeletron, new TaskGroup(4, "Skeleton Hunt", "Defeat skeleton enemies", 32)),
            (BossType.WallOfFlesh, new TaskGroup(5, "Hell Hunt", "Defeat underworld enemies", 40)),
            (BossType.WallOfFlesh, new TaskGroup(5, "Demon Hunt", "Defeat demon enemies", 42)),
            // Hardmode Boss Tasks (Levels 6-12)
            (BossType.QueenSlime, new TaskGroup(6, "Hallow Hunt", "Defeat hallowed enemies", 50)),
            (BossType.QueenSlime, new TaskGroup(6, "Crystal Hunt", "Defeat crystal enemies", 52)),
            (BossType.TheDestroyer, new TaskGroup(7, "Mech Hunt", "Defeat mechanical enemies", 60)),
            (BossType.TheDestroyer, new TaskGroup(7, "Probe Hunt", "Defeat probe enemies", 62)),
            (
                BossType.TheTwins,
                new TaskGroup(7, "Vision Hunt", "Defeat mechanical eye enemies", 60)
            ),
            (BossType.TheTwins, new TaskGroup(7, "Storm Hunt", "Defeat hardmode eye enemies", 62)),
            (
                BossType.SkeletronPrime,
                new TaskGroup(7, "Prime Hunt", "Defeat mechanical enemies", 60)
            ),
            (
                BossType.SkeletronPrime,
                new TaskGroup(7, "Gear Hunt", "Defeat construct enemies", 62)
            ),
            (BossType.Plantera, new TaskGroup(8, "Jungle Hunt", "Defeat jungle enemies", 70)),
            (BossType.Plantera, new TaskGroup(8, "Plant Hunt", "Defeat plant enemies", 72)),
            (BossType.Golem, new TaskGroup(9, "Temple Hunt", "Defeat temple enemies", 80)),
            (BossType.Golem, new TaskGroup(9, "Lihzahrd Hunt", "Defeat temple guard enemies", 82)),
            (BossType.DukeFishron, new TaskGroup(10, "Ocean Hunt", "Defeat ocean enemies", 90)),
            (BossType.DukeFishron, new TaskGroup(10, "Sea Hunt", "Defeat aquatic enemies", 92)),
            (
                BossType.EmpressOfLight,
                new TaskGroup(10, "Light Hunt", "Defeat hallowed enemies", 100)
            ),
            (
                BossType.EmpressOfLight,
                new TaskGroup(10, "Rainbow Hunt", "Defeat fairy enemies", 102)
            ),
            (
                BossType.LunaticCultist,
                new TaskGroup(11, "Cultist Hunt", "Defeat cultist enemies", 110)
            ),
            (
                BossType.LunaticCultist,
                new TaskGroup(11, "Magic Hunt", "Defeat mystic enemies", 112)
            ),
            (
                BossType.MoonLord,
                new TaskGroup(12, "Celestial Hunt", "Defeat celestial enemies", 120)
            ),
            (BossType.MoonLord, new TaskGroup(12, "Lunar Hunt", "Defeat lunar enemies", 122)),
        };

        // Add boss level requirements dictionary
        public static readonly Dictionary<BossType, int> BossLevelRequirements = new()
        {
            // Pre-Hardmode Bosses
            { BossType.KingSlime, 1 }, // First boss
            { BossType.EyeOfCthulhu, 1 }, // Early boss
            { BossType.EaterOfWorlds, 2 }, // Corruption boss
            { BossType.BrainOfCthulhu, 2 }, // Crimson boss
            { BossType.QueenBee, 3 }, // Jungle boss
            { BossType.Deerclops, 3 }, // Snow boss
            { BossType.Skeletron, 4 }, // Dungeon boss
            { BossType.WallOfFlesh, 5 }, // Hell boss
            // Hardmode Bosses
            { BossType.QueenSlime, 6 }, // Early hardmode
            { BossType.TheDestroyer, 7 }, // Mechanical boss
            { BossType.TheTwins, 7 }, // Mechanical boss
            { BossType.SkeletronPrime, 7 }, // Mechanical boss
            { BossType.Plantera, 8 }, // Jungle hardmode
            { BossType.Golem, 9 }, // Temple boss
            { BossType.DukeFishron, 10 }, // Ocean boss
            { BossType.EmpressOfLight, 10 }, // Hallow boss
            { BossType.LunaticCultist, 11 }, // Cultist boss
            { BossType.MoonLord, 12 }, // Final boss
        };

        // Add method to check if boss can spawn at current level
        public static bool CanSpawnBossAtLevel(BossType bossType, int currentLevel)
        {
            if (BossLevelRequirements.TryGetValue(bossType, out int requiredLevel))
            {
                return currentLevel >= requiredLevel;
            }
            return false; // Boss not found in requirements
        }

        public static int GetRequiredXPForLevel(int level)
        {
            if (level <= 0)
                return BASE_XP; // Level 0 has fixed XP requirement
            return (int)(BASE_XP * Math.Pow(LEVEL_MULTIPLIER, level));
        }

        // Update existing GetRequiredLevelForBoss method
        public static int GetRequiredLevelForBoss(BossType bossType)
        {
            if (BossLevelRequirements.TryGetValue(bossType, out int level))
            {
                return level;
            }
            return 0;
        }

        public static TaskGroup? GetAppropriateTaskGroup(int currentLevel, bool isHardMode)
        {
            var availableTasks = BossTasks
                .Where(bt =>
                    bt.Task.RequiredLevel <= currentLevel
                    && // Include level 0 tasks
                    (isHardMode == (bt.Task.RequiredLevel >= 6))
                )
                .OrderByDescending(bt => bt.Task.RequiredLevel)
                .ToList();

            if (!availableTasks.Any())
                return null;

            // For level 0, only return level 0 tasks
            if (currentLevel == 0)
            {
                var level0Tasks = availableTasks.Where(bt => bt.Task.RequiredLevel == 0).ToList();
                return level0Tasks.Count > 0
                    ? level0Tasks[new Random().Next(level0Tasks.Count)].Task
                    : null;
            }

            // For other levels, continue with existing logic
            var highestLevel = availableTasks.First().Task.RequiredLevel;
            var appropriateTasks = availableTasks
                .Where(bt => highestLevel - bt.Task.RequiredLevel <= 2)
                .ToList();

            return appropriateTasks.Count > 0
                ? appropriateTasks[new Random().Next(appropriateTasks.Count)].Task
                : null;
        }

        public static string GetTaskDescription(BossType bossType) =>
            BossTasks.FirstOrDefault(bt => bt.Boss == bossType).Task?.Description ?? string.Empty;
    }
}
