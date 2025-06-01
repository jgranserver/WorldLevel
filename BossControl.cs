using System.Collections.Immutable;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TShockAPI;
using WorldLevel.Models;

namespace WorldLevel
{
    public class BossControl
    {
        private readonly WorldData _worldData;
        private const int MINIMUM_LEVEL_FOR_BOSSES = 1; // Ensure minimum level requirement

        public BossControl(WorldData worldData)
        {
            _worldData = worldData;
            ValidateWorldLevel();
        }

        private void ValidateWorldLevel()
        {
            // Ensure world level is at least 1 for boss progression
            if (_worldData.WorldLevel < MINIMUM_LEVEL_FOR_BOSSES)
            {
                _worldData.WorldLevel = MINIMUM_LEVEL_FOR_BOSSES;
                TShock.Log.Info(
                    $"World level initialized to {MINIMUM_LEVEL_FOR_BOSSES} for boss progression"
                );
            }
        }

        public bool CanSpawnBoss(BossType bossType)
        {
            // Additional validation for world level
            if (_worldData.WorldLevel < MINIMUM_LEVEL_FOR_BOSSES)
            {
                TShock.Log.Debug(
                    $"Boss spawn prevented: World level {_worldData.WorldLevel} below minimum {MINIMUM_LEVEL_FOR_BOSSES}"
                );
                return false;
            }

            return TaskDefinitions.CanSpawnBossAtLevel(bossType, _worldData.WorldLevel);
        }

        public void PreventBossSpawn(TSPlayer player, int npcType)
        {
            var bossType = NPCIdentifier.BossNPCIDs.FirstOrDefault(x => x.Value == npcType).Key;
            var requiredLevel = TaskDefinitions.GetRequiredLevelForBoss(bossType);

            player.SendErrorMessage(
                $"Cannot spawn {bossType} yet! Required world level: {requiredLevel}"
            );
            player.SendInfoMessage($"Current world level: {_worldData.WorldLevel}");
        }
    }
}
