using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using TShockAPI;

namespace WorldLevel.Models
{
    public record MinionGroup(int NpcId, string DisplayName, HashSet<BossType> AssociatedBosses);

    public static class NPCIdentifier
    {
        public static readonly Dictionary<BossType, int> BossNPCIDs = new()
        {
            { BossType.KingSlime, NPCID.KingSlime },
            { BossType.EyeOfCthulhu, NPCID.EyeofCthulhu },
            { BossType.EaterOfWorlds, NPCID.EaterofWorldsHead },
            { BossType.BrainOfCthulhu, NPCID.BrainofCthulhu },
            { BossType.QueenBee, NPCID.QueenBee },
            { BossType.Skeletron, NPCID.SkeletronHead },
            { BossType.WallOfFlesh, NPCID.WallofFlesh },
            { BossType.TheTwins, NPCID.Retinazer },
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

        // Split bosses by progression
        public static readonly Dictionary<BossType, (bool IsHardMode, int NpcId)> BossProgression =
            new()
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

        // Pre-Hardmode Enemy Groups (Based on Wiki categories)
        public static readonly Dictionary<
            string,
            (int[] NpcIds, HashSet<BossType> Bosses)
        > PreHardmodeEnemies = new()
        {
            // Early Game (No boss required)
            {
                "Forest/Surface",
                (
                    new int[]
                    {
                        NPCID.BlueSlime,
                        NPCID.GreenSlime,
                        NPCID.Zombie,
                        NPCID.DemonEye,
                        NPCID.RedSlime,
                        NPCID.YellowSlime,
                        NPCID.BabySlime,
                        NPCID.Raven,
                    },
                    new() { BossType.KingSlime, BossType.EyeOfCthulhu }
                )
            },
            // Early-Mid Game
            {
                "Desert",
                (
                    new[]
                    {
                        NPCID.Antlion,
                        NPCID.WalkingAntlion,
                        NPCID.Vulture,
                        NPCID.SandSlime,
                        (int)NPCID.DesertGhoul,
                    },
                    new() { BossType.KingSlime, BossType.EyeOfCthulhu }
                )
            },
            // Mid Game
            {
                "Corruption",
                (
                    new[]
                    {
                        NPCID.EaterofSouls,
                        NPCID.DevourerHead,
                        NPCID.CorruptBunny,
                        NPCID.Slimer,
                        NPCID.CorruptGoldfish,
                        NPCID.DarkMummy,
                        NPCID.Corruptor,
                        (int)NPCID.CursedHammer,
                    },
                    new HashSet<BossType> { BossType.EaterOfWorlds }
                )
            },
            // Mid Game Alternative
            {
                "Crimson",
                (
                    new[]
                    {
                        NPCID.Crimera,
                        NPCID.FaceMonster,
                        NPCID.BloodCrawler,
                        NPCID.BloodFeeder,
                        NPCID.BloodJelly,
                        NPCID.CrimsonAxe,
                        NPCID.IchorSticker,
                        (int)NPCID.FloatyGross,
                    },
                    new HashSet<BossType> { BossType.BrainOfCthulhu }
                )
            },
            // Mid-Late Game
            {
                "Jungle",
                (
                    new[]
                    {
                        NPCID.Hornet,
                        NPCID.JungleBat,
                        NPCID.JungleSlime,
                        NPCID.Snatcher,
                        NPCID.ManEater,
                        NPCID.Bee,
                        NPCID.JungleCreeper,
                        NPCID.DoctorBones,
                        (int)NPCID.AngryTrapper,
                    },
                    new HashSet<BossType> { BossType.QueenBee }
                )
            },
            // Late Game
            {
                "Dungeon",
                (
                    new[]
                    {
                        NPCID.AngryBones,
                        NPCID.DarkCaster,
                        NPCID.CursedSkull,
                        NPCID.DungeonSlime,
                        NPCID.SkeletonArcher,
                        NPCID.SkeletonCommando,
                        NPCID.TacticalSkeleton,
                        NPCID.BoneThrowingSkeleton,
                        (int)NPCID.RustyArmoredBonesAxe,
                    },
                    new HashSet<BossType> { BossType.Skeletron }
                )
            },
            // End Pre-Hardmode
            {
                "Underworld",
                (
                    new[]
                    {
                        NPCID.Demon,
                        NPCID.VoodooDemon,
                        NPCID.LavaSlime,
                        NPCID.Hellbat,
                        NPCID.FireImp,
                        NPCID.BoneSerpentHead,
                        NPCID.HellArmoredBones,
                        (int)NPCID.RedDevil,
                    },
                    new HashSet<BossType> { BossType.WallOfFlesh }
                )
            },
        };

        // Hardmode Enemy Groups (Based on Wiki categories)
        public static readonly Dictionary<
            string,
            (int[] NpcIds, HashSet<BossType> Bosses)
        > HardmodeEnemies = new()
        {
            {
                "Early Hardmode",
                (
                    new int[]
                    {
                        NPCID.Pixie,
                        NPCID.Wraith,
                        NPCID.WanderingEye,
                        NPCID.PossessedArmor,
                        NPCID.Werewolf,
                        NPCID.GreekSkeleton,
                        NPCID.Mimic,
                        NPCID.IceElemental,
                        NPCID.GiantBat,
                    },
                    new() { BossType.QueenSlime }
                )
            },
            {
                "Mechanical",
                (
                    new int[]
                    {
                        NPCID.Probe,
                        NPCID.IlluminantBat,
                        NPCID.ArmoredSkeleton,
                        NPCID.CursedHammer,
                        NPCID.GiantCursedSkull,
                        NPCID.Mimic,
                        NPCID.IchorSticker,
                        NPCID.Clinger,
                        NPCID.AngryTrapper,
                    },
                    new() { BossType.TheDestroyer, BossType.TheTwins, BossType.SkeletronPrime }
                )
            },
            {
                "Hallow",
                (
                    new int[]
                    {
                        NPCID.Pixie,
                        NPCID.Unicorn,
                        NPCID.ChaosElemental,
                        NPCID.EnchantedSword,
                        NPCID.IlluminantBat,
                        NPCID.LightMummy,
                        NPCID.Gastropod,
                        NPCID.RainbowSlime,
                        NPCID.BigMimicHallow,
                    },
                    new() { BossType.QueenSlime, BossType.EmpressOfLight }
                )
            },
            {
                "Underground Jungle",
                (
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
                        (int)NPCID.ToxicSludge,
                    },
                    new HashSet<BossType>() { BossType.Plantera }
                )
            },
            {
                "Temple",
                (
                    new[]
                    {
                        NPCID.FlyingSnake,
                        NPCID.LihzahrdCrawler,
                        NPCID.Lihzahrd,
                        NPCID.DesertDjinn,
                        NPCID.AngryTrapper,
                        NPCID.FlyingAntlion,
                        NPCID.WalkingAntlion,
                        (int)NPCID.GiantFlyingAntlion,
                    },
                    new() { BossType.Golem }
                )
            },
            {
                "Ocean",
                (
                    new[]
                    {
                        NPCID.Shark,
                        NPCID.AnglerFish,
                        NPCID.BloodSquid,
                        NPCID.GoblinShark,
                        NPCID.BloodNautilus,
                        NPCID.IceTortoise,
                        NPCID.SeaSnail,
                        (int)NPCID.Drippler,
                    },
                    new() { BossType.DukeFishron }
                )
            },
            {
                "Celestial",
                (
                    new int[]
                    {
                        NPCID.StardustSoldier,
                        NPCID.SolarCrawltipedeHead,
                        NPCID.NebulaBrain,
                        NPCID.VortexHornet,
                        NPCID.LunarTowerStardust,
                        NPCID.LunarTowerSolar,
                        NPCID.LunarTowerVortex,
                        NPCID.LunarTowerNebula,
                        NPCID.CultistArcherBlue,
                    },
                    new() { BossType.MoonLord }
                )
            },
        };
    }
}
