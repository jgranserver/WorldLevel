using System;
using System.Collections.Generic;

namespace WorldLevel.Models
{
    public class WorldData
    {
        public int WorldLevel { get; set; } = 0;
        public int CurrentXP { get; set; } = 0;
        public int RequiredXP { get; set; }
        public ActiveTask? CurrentTask { get; set; }
        public DateTime? LastBossUnlock { get; set; }
        public List<int> UnlockedBosses { get; set; } = new();
        public Dictionary<int, PlayerRerollData> PlayerRerolls { get; set; } = new();

        public void SetRequiredXP(int xp)
        {
            RequiredXP = xp;
        }
    }

    public class PlayerRerollData
    {
        public DateTime LastRerollTime { get; set; }
        public int DailyRerollCount { get; set; }
        public DateTime LastResetTime { get; set; }

        public bool CanReroll(int cooldownMinutes)
        {
            return (DateTime.Now - LastRerollTime).TotalMinutes >= cooldownMinutes;
        }

        public void UpdateReroll()
        {
            if (DateTime.UtcNow.Date > LastResetTime.Date)
            {
                DailyRerollCount = 1;
                LastResetTime = DateTime.UtcNow;
            }
            else
            {
                DailyRerollCount++;
            }
            LastRerollTime = DateTime.Now;
        }
    }

    public class ActiveTask
    {
        public string TaskType { get; set; } = string.Empty;
        public int TargetMobId { get; set; }
        public int Progress { get; set; } = 0;
        public int Goal { get; set; } = 0;
        public int RewardXP { get; set; } = 0;
        public string AssociatedBoss { get; set; } = string.Empty;
    }
}
