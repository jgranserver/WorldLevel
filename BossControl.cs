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

        public BossControl(WorldData worldData)
        {
            _worldData = worldData;
        }

        public bool CanSpawnBoss(BossType bossType)
        {
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
