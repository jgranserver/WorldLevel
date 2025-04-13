using System;
using TShockAPI;

namespace WorldLevel.Models
{
    public class PlayerBankAccount
    {
        public int AccountId { get; set; }
        public string PlayerName { get; set; }
        public long Balance { get; set; }
        public DateTime LastTransaction { get; set; }

        public PlayerBankAccount(int accountId, string playerName, long balance)
        {
            AccountId = accountId;
            PlayerName = playerName;
            Balance = balance;
            LastTransaction = DateTime.UtcNow;
        }
    }
}
