using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using TShockAPI;

namespace WorldLevel.Models
{
    public record MinionGroup(int NpcId, string DisplayName, HashSet<BossType> AssociatedBosses);

    public static class NPCIdentifier
    {
        private static readonly Dictionary<BossType, int> _bossNpcIds;
        private static readonly Dictionary<BossType, (bool IsHardMode, int NpcId)> _bossProgression;

        static NPCIdentifier()
        {
            // Initialize boss ID mappings
            _bossNpcIds = new()
            {
                { BossType.KingSlime, NPCID.KingSlime },
                { BossType.EyeOfCthulhu, NPCID.EyeofCthulhu },
                { BossType.EaterOfWorlds, NPCID.EaterofWorldsHead },
                { BossType.BrainOfCthulhu, NPCID.BrainofCthulhu },
                { BossType.QueenBee, NPCID.QueenBee },
                { BossType.Skeletron, NPCID.SkeletronHead },
                { BossType.WallOfFlesh, NPCID.WallofFlesh },
                { BossType.TheTwins, NPCID.Retinazer }, // Primary ID for The Twins
                { BossType.TheDestroyer, NPCID.TheDestroyer },
                { BossType.SkeletronPrime, NPCID.SkeletronPrime },
                { BossType.Plantera, NPCID.Plantera },
                { BossType.Golem, NPCID.Golem },
                { BossType.DukeFishron, NPCID.DukeFishron },
                { BossType.LunaticCultist, NPCID.CultistBoss },
                { BossType.MoonLord, NPCID.MoonLordCore },
                { BossType.Deerclops, NPCID.Deerclops },
                { BossType.QueenSlime, NPCID.QueenSlimeBoss },
                { BossType.EmpressOfLight, NPCID.HallowBoss },
            };

            // Initialize progression data
            _bossProgression = new()
            {
                // Pre-Hardmode Bosses (Ordered by progression)
                { BossType.KingSlime, (false, NPCID.KingSlime) },
                { BossType.EyeOfCthulhu, (false, NPCID.EyeofCthulhu) },
                { BossType.EaterOfWorlds, (false, NPCID.EaterofWorldsHead) },
                { BossType.BrainOfCthulhu, (false, NPCID.BrainofCthulhu) },
                { BossType.QueenBee, (false, NPCID.QueenBee) },
                { BossType.Skeletron, (false, NPCID.SkeletronHead) },
                { BossType.Deerclops, (false, NPCID.Deerclops) },
                { BossType.WallOfFlesh, (false, NPCID.WallofFlesh) },
                // Hardmode Bosses (Ordered by progression)
                { BossType.QueenSlime, (true, NPCID.QueenSlimeBoss) },
                { BossType.TheDestroyer, (true, NPCID.TheDestroyer) },
                { BossType.TheTwins, (true, NPCID.Retinazer) },
                { BossType.SkeletronPrime, (true, NPCID.SkeletronPrime) },
                { BossType.Plantera, (true, NPCID.Plantera) },
                { BossType.Golem, (true, NPCID.Golem) },
                { BossType.DukeFishron, (true, NPCID.DukeFishron) },
                { BossType.EmpressOfLight, (true, NPCID.HallowBoss) },
                { BossType.LunaticCultist, (true, NPCID.CultistBoss) },
                { BossType.MoonLord, (true, NPCID.MoonLordCore) },
            };
        }

        // Add method to check for both Twins
        public static bool IsTheTwinsBoss(int npcId) =>
            npcId == NPCID.Retinazer || npcId == NPCID.Spazmatism;

        // Create enemy group helper method
        private static (int[] NpcIds, HashSet<BossType> Bosses) CreateEnemyGroup(
            int[] npcIds,
            params BossType[] bosses
        ) => (npcIds, new HashSet<BossType>(bosses));

        // Pre-Hardmode Enemy Groups
        public static readonly Dictionary<
            string,
            (int[] NpcIds, HashSet<BossType> Bosses)
        > PreHardmodeEnemies = new()
        {
            ["Forest/Surface"] = CreateEnemyGroup(
                new[]
                {
                    1,
                    2,
                    -43,
                    190,
                    -38,
                    191,
                    -39,
                    192,
                    -40,
                    193,
                    -41,
                    194,
                    -42,
                    624,
                    73,
                    -3,
                    632,
                    23,
                    -4,
                    -7,
                    301,
                    -8,
                    3,
                    -26,
                    -27,
                    430,
                    132,
                    -28,
                    -29,
                    186,
                    -30,
                    -31,
                    187,
                    -32,
                    -33,
                    188,
                    -34,
                    -35,
                    189,
                    -36,
                    -37,
                    200,
                    -44,
                    -45,
                    590,
                },
                BossType.KingSlime,
                BossType.EyeOfCthulhu
            ),
            ["Desert"] = CreateEnemyGroup(
                new[] { 69, 582, 580, 508, 581, 509, 537, 513, 61 },
                BossType.KingSlime,
                BossType.EyeOfCthulhu
            ),
            ["Corruption"] = CreateEnemyGroup(new[] { 7, 6, -11, -12 }, BossType.EaterOfWorlds),
            ["Crimson"] = CreateEnemyGroup(
                new[] { 239, 240, 173, -22, -23, 181 },
                BossType.BrainOfCthulhu
            ),
            ["Snow"] = CreateEnemyGroup(
                new[] { 218, 52, 161, 431, 150, 147, 185, 184, 167 },
                BossType.Deerclops
            ),
            ["Jungle"] = CreateEnemyGroup(
                new[]
                {
                    210,
                    211,
                    42,
                    -16,
                    -17,
                    231,
                    -56,
                    -57,
                    232,
                    -58,
                    -59,
                    233,
                    -60,
                    -61,
                    234,
                    -62,
                    -63,
                    235,
                    -64,
                    -65,
                    51,
                    -10,
                    219,
                    43,
                    58,
                    56,
                    204,
                },
                BossType.QueenBee
            ),
            ["Mushroom"] = CreateEnemyGroup(
                new[] { 257, 259, 258, 634, 635, 254, 255 },
                BossType.QueenBee
            ),
            ["Ocean"] = CreateEnemyGroup(new[] { 64, 67, 65 }, BossType.Skeletron),
            ["Caverns"] = CreateEnemyGroup(
                new[]
                {
                    -5,
                    -6,
                    63,
                    49,
                    217,
                    494,
                    495,
                    316,
                    496,
                    497,
                    10,
                    483,
                    482,
                    481,
                    16,
                    196,
                    498,
                    499,
                    500,
                    501,
                    502,
                    503,
                    504,
                    505,
                    506,
                    676,
                    471,
                    44,
                    164,
                    -9,
                },
                BossType.Skeletron
            ),
            ["Floating Island"] = CreateEnemyGroup(new[] { 48 }, BossType.Skeletron),
            ["Dungeon"] = CreateEnemyGroup(
                new[] { 31, -13, -14, 294, 295, 296, 34, 32, 71 },
                BossType.WallOfFlesh
            ),
            ["Underworld"] = CreateEnemyGroup(
                new[] { 39, 62, 24, 60, 59, 66 },
                BossType.WallOfFlesh
            ),
        };

        // Hardmode Enemy Groups
        public static readonly Dictionary<
            string,
            (int[] NpcIds, HashSet<BossType> Bosses)
        > HardmodeEnemies = new()
        {
            ["Early Hardmode"] = CreateEnemyGroup(
                new[]
                {
                    532,
                    NPCID.BlackRecluse,
                    NPCID.BloodFeeder,
                    NPCID.BloodJelly,
                    NPCID.BloodMummy,
                    NPCID.Clinger,
                    NPCID.CorruptSlime,
                    NPCID.Corruptor,
                    NPCID.Crimslime,
                    NPCID.DarkMummy,
                    NPCID.DesertDjinn,
                    NPCID.FloatyGross,
                    NPCID.FungoFish,
                    NPCID.Gastropod,
                    NPCID.PossessedArmor,
                    NPCID.Wraith,
                    NPCID.Werewolf,
                },
                BossType.QueenSlime
            ),
            ["Mechanical"] = CreateEnemyGroup(
                new[]
                {
                    NPCID.ArmoredSkeleton,
                    NPCID.ArmoredViking,
                    NPCID.BlueArmoredBones,
                    NPCID.BoneLee,
                    NPCID.BigMimicCorruption,
                    NPCID.BigMimicCrimson,
                    NPCID.BigMimicHallow,
                    NPCID.CrimsonAxe,
                    NPCID.CursedHammer,
                    NPCID.EnchantedSword,
                    NPCID.DesertGhoulCorruption,
                    NPCID.DesertGhoulCrimson,
                    (int)NPCID.DesertGhoulHallow,
                },
                BossType.TheDestroyer,
                BossType.TheTwins,
                BossType.SkeletronPrime
            ),
            ["Underground Jungle"] = CreateEnemyGroup(
                new[]
                {
                    NPCID.Moth,
                    NPCID.GiantTortoise,
                    NPCID.AngryTrapper,
                    NPCID.Derpling,
                    NPCID.HornetFatty,
                    NPCID.GiantFlyingFox,
                    NPCID.MossHornet,
                    NPCID.Arapaima,
                    NPCID.AnglerFish,
                    (int)NPCID.ToxicSludge,
                },
                BossType.Plantera
            ),
            ["Hallow"] = CreateEnemyGroup(
                new[]
                {
                    NPCID.ChaosElemental,
                    NPCID.IlluminantBat,
                    NPCID.IlluminantSlime,
                    NPCID.LightMummy,
                    NPCID.Pixie,
                    (int)NPCID.Unicorn,
                },
                BossType.DukeFishron,
                BossType.EmpressOfLight
            ),
            ["Temple"] = CreateEnemyGroup(
                new[]
                {
                    NPCID.FlyingSnake,
                    NPCID.LihzahrdCrawler,
                    NPCID.Lihzahrd,
                    NPCID.DesertDjinn,
                    NPCID.TacticalSkeleton,
                    NPCID.SkeletonSniper,
                    NPCID.RustyArmoredBonesAxe,
                    NPCID.SkeletonCommando,
                    NPCID.RockGolem,
                    NPCID.RedDevil,
                    NPCID.MothronSpawn,
                    NPCID.Butcher,
                    NPCID.CreatureFromTheDeep,
                    NPCID.DeadlySphere,
                    NPCID.DrManFly,
                    NPCID.Eyezor,
                    NPCID.Frankenstein,
                    NPCID.Fritz,
                    NPCID.Nailhead,
                    NPCID.Psycho,
                    NPCID.Reaper,
                    NPCID.SwampThing,
                    NPCID.ThePossessed,
                    NPCID.Vampire,
                    NPCID.Hellhound,
                    NPCID.Poltergeist,
                    (int)NPCID.Splinterling,
                },
                BossType.Golem
            ),
            ["Celestial"] = CreateEnemyGroup(
                new[]
                {
                    NPCID.BrainScrambler,
                    NPCID.GigaZapper,
                    NPCID.GrayGrunt,
                    NPCID.MartianEngineer,
                    NPCID.MartianOfficer,
                    NPCID.MartianWalker,
                    NPCID.RayGunner,
                    NPCID.Scutlix,
                    NPCID.ScutlixRider,
                    NPCID.MartianTurret,
                    NPCID.NebulaBeast,
                    NPCID.NebulaHeadcrab,
                    NPCID.NebulaSoldier,
                    NPCID.NebulaBrain,
                    NPCID.SolarCorite,
                    NPCID.SolarSroller,
                    NPCID.SolarCrawltipedeHead,
                    NPCID.SolarDrakomire,
                    NPCID.SolarDrakomireRider,
                    NPCID.SolarSolenian,
                    NPCID.VortexHornet,
                    NPCID.VortexHornetQueen,
                    NPCID.VortexLarva,
                    NPCID.VortexRifleman,
                    NPCID.VortexSoldier,
                    NPCID.StardustCellBig,
                    NPCID.StardustSoldier,
                    (int)NPCID.StardustWormHead,
                },
                BossType.MoonLord
            ),
        };

        // Public accessors
        public static IReadOnlyDictionary<BossType, int> BossNPCIDs => _bossNpcIds;
        public static IReadOnlyDictionary<BossType, (bool IsHardMode, int NpcId)> BossProgression =>
            _bossProgression;

        // Helper methods
        public static bool IsBossNPC(int npcId) =>
            _bossNpcIds.ContainsValue(npcId)
            || (npcId == NPCID.Spazmatism && _bossNpcIds.ContainsValue(NPCID.Retinazer));

        public static bool IsHardmodeBoss(BossType bossType) =>
            _bossProgression.TryGetValue(bossType, out var info) && info.IsHardMode;
    }
}
