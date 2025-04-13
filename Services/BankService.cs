using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JgransEconomySystem;
using TShockAPI;
using WorldLevel.Models;

namespace WorldLevel.Services
{
    public class BankService
    {
        private readonly EconomyDatabase _economy;

        public BankService()
        {
            _economy = new EconomyDatabase(
                Path.Combine(TShock.SavePath, "JgransEconomyBanks.sqlite")
            );
        }

        public async Task<bool> UpdateBalance(TSPlayer player, int amount, string reason)
        {
            try
            {
                var newBalance = await _economy.GetCurrencyAmount(player.Account.ID) + amount;

                // Save new balance
                await _economy.SaveCurrencyAmount(player.Account.ID, newBalance);

                // Send appropriate message
                if (amount > 0)
                {
                    player.SendSuccessMessage($"Received {amount:N0} jspoints for {reason}");
                    TShock.Log.Debug(
                        $"Added {amount:N0} jspoints to {player.Name}'s account. New balance: {newBalance:N0}"
                    );
                }
                else if (amount < 0)
                {
                    player.SendInfoMessage($"Spent {Math.Abs(amount):N0} jspoints for {reason}");
                    TShock.Log.Debug(
                        $"Removed {Math.Abs(amount):N0} jspoints from {player.Name}'s account. New balance: {newBalance:N0}"
                    );
                }

                return true;
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Failed to update balance for {player.Name}: {ex.Message}");
                TShock.Log.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task DistributeTaskRewards(
            Dictionary<int, TaskContribution> contributions,
            int totalReward
        )
        {
            if (!contributions.Any())
                return;

            int totalKills = contributions.Values.Sum(c => c.Kills);

            foreach (var contribution in contributions.Values)
            {
                if (contribution.Kills > 0)
                {
                    contribution.ContributionPercentage = (double)contribution.Kills / totalKills;
                    contribution.RewardAmount = (int)(
                        totalReward * contribution.ContributionPercentage
                    );

                    var player = TShock.Players.FirstOrDefault(p =>
                        p?.Account?.ID == contribution.PlayerID
                    );
                    if (player != null)
                    {
                        await UpdateBalance(
                            player,
                            contribution.RewardAmount,
                            $"contributing {contribution.Kills} kills ({(contribution.ContributionPercentage * 100):F1}%) to task completion"
                        );
                    }
                }
            }
        }
    }
}
