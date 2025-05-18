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
        public DateTime NextRerollReset { get; set; } = DateTime.UtcNow.Date.AddDays(1);
        public Dictionary<int, PlayerRerollData> PlayerRerolls { get; set; } = new();
        public Queue<int> RecentTaskNPCs { get; set; } = new Queue<int>(5); // Tracks last 5 tasks

        public void SetRequiredXP(int xp)
        {
            RequiredXP = xp;
        }

        public void AddRecentTask(int npcId)
        {
            if (RecentTaskNPCs.Count >= 5)
                RecentTaskNPCs.Dequeue();
            RecentTaskNPCs.Enqueue(npcId);
        }
    }

    public class PlayerRerollData
    {
        public DateTime LastRerollTime { get; set; }
        public int RerollsUsed { get; set; }

        public bool CanReroll(int cooldownMinutes)
        {
            return (DateTime.Now - LastRerollTime).TotalMinutes >= cooldownMinutes;
        }

        public void UpdateReroll()
        {
            if (DateTime.UtcNow.Date > LastRerollTime.Date)
            {
                RerollsUsed = 1;
                LastRerollTime = DateTime.UtcNow;
            }
            else
            {
                RerollsUsed++;
            }
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
