using System;
using System.IO;
using System.Text.Json;
using TShockAPI;
using WorldLevel.Models;

namespace WorldLevel.Services
{
    public class NPCRarityService
    {
        private readonly NPCRarityConfig _config;
        private readonly string _configPath;

        public NPCRarityService()
        {
            _configPath = Path.Combine(TShock.SavePath, "npc-rarity.json");
            _config = LoadConfig();
        }

        public NPCRarity GetNPCRarity(int npcId)
        {
            if (_config.SpecialNPCs.Contains(npcId))
                return NPCRarity.Special;
            if (_config.HostileNPCs.Contains(npcId))
                return NPCRarity.Hostile;
            if (_config.SuperRareNPCs.Contains(npcId))
                return NPCRarity.SuperRare;
            return NPCRarity.Normal;
        }

        public int GetRequiredKills(NPCRarity rarity)
        {
            int min = _config.RequiredKillsMin.TryGetValue(rarity, out int minVal) ? minVal : 1;
            int max = _config.RequiredKillsMax.TryGetValue(rarity, out int maxVal) ? maxVal : 100;
            var rand = new System.Random();
            return rand.Next(min, max + 1);
        }

        public double GetXPMultiplier(int npcId)
        {
            // Use the config's method directly
            return _config.GetXPMultiplier(npcId);
        }

        private NPCRarityConfig LoadConfig()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    var defaultConfig = new NPCRarityConfig();
                    Directory.CreateDirectory(Path.GetDirectoryName(_configPath));
                    File.WriteAllText(
                        _configPath,
                        JsonSerializer.Serialize(
                            defaultConfig,
                            new JsonSerializerOptions { WriteIndented = true }
                        )
                    );
                    return defaultConfig;
                }

                return JsonSerializer.Deserialize<NPCRarityConfig>(File.ReadAllText(_configPath));
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error loading NPC rarity config: {ex.Message}");
                return new NPCRarityConfig();
            }
        }
    }
}
