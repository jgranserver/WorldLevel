using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using WorldLevel.Models;
using WorldLevel.Services;

namespace WorldLevel
{
    [ApiVersion(2, 1)]
    public class WorldLevelPlugin : TerrariaPlugin
    {
        private WorldData _worldData;
        private TaskManager? _taskManager = null;
        private BossControl? _bossControl = null;
        private string SavePath => Path.Combine(TShock.SavePath, "worldlevel.json");
        private DateTime _lastTaskBroadcast = DateTime.MinValue;
        private const int BROADCAST_COOLDOWN_SECONDS = 300;
        private const int REROLL_COST = 50000; // Cost in jspoints
        private const int REROLL_COOLDOWN_MINUTES = 5;
        private const int DAILY_REROLL_LIMIT = 10; // Maximum rerolls per day
        private readonly BankService _bankService;

        public override string Name => "World Level";
        public override Version Version => new Version(1, 1, 3);
        public override string Author => "jgranserver";
        public override string Description => "A world leveling system with tasks and boss unlocks";

        public WorldLevelPlugin(Main game)
            : base(game)
        {
            _worldData = new WorldData();
            _bankService = new BankService();
        }

        public override void Initialize()
        {
            LoadWorldData();
            _taskManager = new TaskManager(_worldData);
            _bossControl = new BossControl(_worldData);

            // Register hooks
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
            ServerApi.Hooks.NpcKilled.Register(this, OnNPCKill);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);

            // Register commands
            Commands.ChatCommands.Add(new Command("worldlevel", WorldLevelCmd, "worldlevel", "wl"));
            TShock.Groups.AddPermissions("default", new List<string> { "worldlevel" });
        }

        private void LoadWorldData()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var json = File.ReadAllText(SavePath);
                    _worldData = JsonSerializer.Deserialize<WorldData>(json) ?? new WorldData();
                }
                else
                {
                    _worldData = new WorldData
                    {
                        WorldLevel = 1, // Start at level 1 instead of 0
                        NextRerollReset = DateTime.UtcNow.Date.AddDays(1),
                        PlayerRerolls = new Dictionary<int, PlayerRerollData>(),
                    };
                }

                // Ensure minimum world level
                if (_worldData.WorldLevel < 1)
                {
                    _worldData.WorldLevel = 1;
                    TShock.Log.Info("World level initialized to 1");
                }

                SaveWorldData(); // Save any updates
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Failed to load world data: {ex.Message}");
                _worldData = new WorldData
                {
                    WorldLevel = 1, // Ensure minimum level even on error
                    NextRerollReset = DateTime.UtcNow.Date.AddDays(1),
                    PlayerRerolls = new Dictionary<int, PlayerRerollData>(),
                };
            }
        }

        private void SaveWorldData()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_worldData, options);
                File.WriteAllText(SavePath, json);
                TShock.Log.Debug("World data saved successfully");
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Failed to save world data: {ex.Message}");
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            _taskManager?.Update();
            CheckAndResetRerolls(); // Add reset check to update loop
        }

        private void OnNPCKill(NpcKilledEventArgs args)
        {
            try
            {
                if (args.npc?.active != true || args.npc.friendly || args.npc.townNPC)
                    return;

                var npcId = args.npc.netID;
                TShock.Log.Debug($"NPC Killed - ID: {npcId}, Name: {Lang.GetNPCNameValue(npcId)}");

                // Early return if no active task
                if (_worldData.CurrentTask == null)
                    return;

                // Get the killer player
                var player = TShock.Players.FirstOrDefault(p =>
                    p?.Active == true && p.Index == args.npc.target
                );

                if (player == null)
                {
                    TShock.Log.Debug(
                        $"No valid player found for kill. Target index: {args.npc.target}"
                    );
                    return;
                }

                // Check broadcast cooldown
                bool canBroadcast =
                    (DateTime.Now - _lastTaskBroadcast).TotalSeconds >= BROADCAST_COOLDOWN_SECONDS;

                // Handle the kill
                _taskManager?.HandleNpcKill(npcId, player, canBroadcast);

                if (canBroadcast)
                {
                    _lastTaskBroadcast = DateTime.Now;
                }

                SaveWorldData();
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error in OnNPCKill: {ex}");
                TShock.Log.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.Handled)
            {
                TShock.Log.Debug("Event already handled, skipping boss spawn check");
                return;
            }

            var player = TShock.Players[args.Msg.whoAmI];
            if (player == null)
            {
                TShock.Log.Debug("Player is null, skipping boss spawn check");
                return;
            }

            if (args.MsgID == PacketTypes.SpawnBossorInvasion)
            {
                TShock.Log.Debug($"Boss spawn attempt by {player.Name}");
                TShock.Log.Debug($"Current World Level: {_worldData.WorldLevel}");
                TShock.Log.Debug($"Packet Length: {args.Length}");
                HandleBossSpawn(args, player);
            }
        }

        private void HandleBossSpawn(GetDataEventArgs args, TSPlayer player)
        {
            try
            {
                if (args.Length < 4)
                {
                    TShock.Log.Debug("Packet too short for boss spawn");
                    args.Handled = true;
                    return;
                }

                using var reader = new BinaryReader(
                    new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)
                );

                short playerIndex = reader.ReadInt16();
                short npcType = reader.ReadInt16();

                // Verify player index
                if (playerIndex != player.Index)
                {
                    TShock.Log.Debug(
                        $"Player index mismatch: got {playerIndex}, expected {player.Index}"
                    );
                    args.Handled = true;
                    return;
                }

                TShock.Log.Debug($"Boss/Invasion spawn attempt - NPC Type: {npcType}");

                // Special handling for The Twins
                if (npcType == NPCID.Spazmatism || npcType == NPCID.Retinazer)
                {
                    var bossType = BossType.TheTwins;
                    TShock.Log.Debug(
                        $"Twins spawn attempt: {(npcType == NPCID.Spazmatism ? "Spazmatism" : "Retinazer")}"
                    );

                    if (!_bossControl.CanSpawnBoss(bossType))
                    {
                        args.Handled = true;
                        _bossControl.PreventBossSpawn(player, npcType);
                        TShock.Log.Debug($"Twins spawn prevented at level {_worldData.WorldLevel}");
                        return;
                    }

                    TShock.Log.Debug($"Twins spawn allowed");
                    return;
                }

                // Handle other bosses
                if (npcType > 0 && NPCIdentifier.BossNPCIDs.Values.Contains(npcType))
                {
                    var bossType = NPCIdentifier
                        .BossNPCIDs.FirstOrDefault(x => x.Value == npcType)
                        .Key;

                    TShock.Log.Debug($"Boss type identified: {bossType}");
                    TShock.Log.Debug($"Current world level: {_worldData.WorldLevel}");

                    if (!_bossControl.CanSpawnBoss(bossType))
                    {
                        args.Handled = true;
                        _bossControl.PreventBossSpawn(player, npcType);
                        TShock.Log.Debug(
                            $"Boss spawn prevented for {bossType} at level {_worldData.WorldLevel}"
                        );
                        return;
                    }

                    TShock.Log.Debug($"Boss spawn allowed for {bossType}");
                }
                else
                {
                    TShock.Log.Debug($"Non-boss NPC type: {npcType}");
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error handling boss spawn: {ex}");
                args.Handled = true;
            }
        }

        private void WorldLevelCmd(CommandArgs args)
        {
            var player = args.Player;

            if (args.Parameters.Count > 0)
            {
                switch (args.Parameters[0].ToLower())
                {
                    case "help":
                        ShowHelp(player);
                        return;
                    case "status":
                        ShowStatus(player);
                        return;
                    case "task":
                        ShowCurrentTask(player);
                        return;
                    case "admin":
                        if (player.HasPermission("worldlevel.admin"))
                            HandleAdminCommands(player, args.Parameters.Skip(1).ToArray());
                        else
                            player.SendErrorMessage(
                                "You don't have permission to use admin commands!"
                            );
                        return;
                    case "reroll":
                        HandleTaskReroll(player);
                        return;
                    default:
                        player.SendErrorMessage(
                            "Unknown command. Use /worldlevel help for commands."
                        );
                        return;
                }
            }

            // Default behavior - show basic info
            ShowStatus(player);
        }

        private void ShowHelp(TSPlayer player)
        {
            player.SendMessage("╔══════ World Level Commands ══════╗", Color.LightGreen);
            player.SendMessage("║ /worldlevel - Show current status", Color.White);
            player.SendMessage("║ /wl status - Show detailed progress", Color.White);
            player.SendMessage("║ /wl task - Show current task details", Color.White);
            player.SendMessage(
                "║ /wl reroll - Reroll current task (50000 jspoints, 10/day)",
                Color.White
            );

            if (player.HasPermission("worldlevel.admin"))
            {
                player.SendMessage("╠══════ Admin Commands ══════╣", Color.Orange);
                player.SendMessage("║ /wl admin setlevel <level>", Color.White);
                player.SendMessage("║ /wl admin addxp <amount>", Color.White);
                player.SendMessage("║ /wl admin newtask", Color.White);
                player.SendMessage("║ /wl admin updatexp", Color.White);
            }
            player.SendMessage("╚════════════════════════════╝", Color.LightGreen);
        }

        private void ShowStatus(TSPlayer player)
        {
            try
            {
                var progressPercent =
                    _worldData.RequiredXP > 0
                        ? (_worldData.CurrentXP * 100) / _worldData.RequiredXP
                        : 0;

                var currentXP = _worldData.CurrentXP.ToString("N0");
                var requiredXP = _worldData.RequiredXP.ToString("N0");
                var remainingXP = (_worldData.RequiredXP - _worldData.CurrentXP).ToString("N0");

                player.SendMessage("╔══════ World Status ══════╗", Color.Gold);
                player.SendMessage($"║ Level: {_worldData.WorldLevel}", Color.LightGreen);
                player.SendMessage($"║ XP: {currentXP}/{requiredXP}", Color.Yellow);
                player.SendMessage($"║ Progress: {progressPercent}%", Color.Orange);
                player.SendMessage($"║ Remaining: {remainingXP} XP", Color.LightBlue);
                player.SendMessage(
                    $"║ State: {(Main.hardMode ? "Hardmode" : "Pre-Hardmode")}",
                    Main.hardMode ? Color.Red : Color.Green
                );
                player.SendMessage("╚════════════════════════╝", Color.Gold);
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error showing status: {ex.Message}");
                player.SendErrorMessage("An error occurred while showing world status.");
            }
        }

        private void ShowCurrentTask(TSPlayer player)
        {
            if (_worldData.CurrentTask == null)
            {
                player.SendMessage("╔══════ Current Task ══════╗", Color.Gray);
                player.SendMessage("║ No active task available", Color.White);
                player.SendMessage("║ A new one will generate soon!", Color.LightGray);
                player.SendMessage("╚════════════════════════╝", Color.Gray);
                return;
            }

            var task = _worldData.CurrentTask;
            var bossType = Enum.TryParse<BossType>(task.AssociatedBoss, out var parsedBossType)
                ? parsedBossType
                : BossType.Unknown;

            // Get task description from TaskDefinitions
            var taskGroup = TaskDefinitions.BossTasks.FirstOrDefault(bt =>
                bt.Boss == bossType && bt.Task.RequiredLevel == _worldData.WorldLevel
            );

            var npcName = Lang.GetNPCNameValue(task.TargetMobId);
            var progressPercent = (task.Progress * 100) / task.Goal;

            player.SendMessage("╔══════ Current Task ══════╗", Color.LightBlue);
            player.SendMessage(
                $"║ {(taskGroup != default ? taskGroup.Task.Description : "Active Task")}",
                Color.Yellow
            );
            player.SendMessage($"║ Target: {npcName}", Color.White);
            if (!taskGroup.Equals(default((BossType Boss, TaskGroup Task))))
            {
                player.SendMessage(
                    $"║ Objective: {taskGroup.Task.MinionDescription}",
                    Color.LightGray
                );
            }
            player.SendMessage(
                $"║ Progress: {task.Progress}/{task.Goal} ({progressPercent}%)",
                progressPercent >= 50 ? Color.LightGreen : Color.Yellow
            );
            player.SendMessage($"║ Reward: {task.RewardXP:N0} XP", Color.Orange);
            player.SendMessage("╚════════════════════════╝", Color.LightBlue);
        }

        private void HandleAdminCommands(TSPlayer player, string[] args)
        {
            if (args.Length < 1)
            {
                player.SendErrorMessage(
                    "Invalid admin command. Use /worldlevel help for commands."
                );
                return;
            }

            switch (args[0].ToLower())
            {
                case "setlevel":
                    if (args.Length < 2 || !int.TryParse(args[1], out int level))
                    {
                        player.SendErrorMessage("Usage: /wl admin setlevel <level>");
                        return;
                    }
                    _worldData.WorldLevel = level;
                    player.SendSuccessMessage($"World level set to {level}");
                    break;

                case "addxp":
                    if (args.Length < 2 || !int.TryParse(args[1], out int xp))
                    {
                        player.SendErrorMessage("Usage: /wl admin addxp <amount>");
                        return;
                    }

                    // Add XP and handle level ups
                    _worldData.CurrentXP += xp;
                    while (_worldData.CurrentXP >= _worldData.RequiredXP)
                    {
                        var overflow = _worldData.CurrentXP - _worldData.RequiredXP;
                        _worldData.WorldLevel++;
                        _worldData.CurrentXP = overflow;
                        _worldData.RequiredXP = TaskDefinitions.GetRequiredXPForLevel(
                            _worldData.WorldLevel
                        );

                        // Broadcast level up
                        TShock.Utils.Broadcast(
                            $"World has reached level {_worldData.WorldLevel}!",
                            Color.LightGreen
                        );
                    }

                    player.SendSuccessMessage(
                        $"Added {xp} XP to world (Current: {_worldData.CurrentXP}/{_worldData.RequiredXP})"
                    );
                    break;

                case "newtask":
                    _worldData.CurrentTask = null;
                    _taskManager?.Update();
                    player.SendSuccessMessage("Generating new task...");
                    break;

                case "updatexp":
                    // Update XP requirements for current level
                    _worldData.RequiredXP = TaskDefinitions.GetRequiredXPForLevel(
                        _worldData.WorldLevel
                    );

                    // Show the update results
                    player.SendSuccessMessage($"XP requirements updated!");
                    player.SendInfoMessage($"Current World Level: {_worldData.WorldLevel}");
                    player.SendInfoMessage(
                        $"Current Progress: {_worldData.CurrentXP:N0}/{_worldData.RequiredXP:N0} XP"
                    );

                    // Show next few level requirements
                    for (int i = _worldData.WorldLevel + 1; i <= _worldData.WorldLevel + 3; i++)
                    {
                        int nextLevelXP = TaskDefinitions.GetRequiredXPForLevel(i);
                        player.SendInfoMessage($"Level {i} requires: {nextLevelXP:N0} XP");
                    }

                    SaveWorldData();
                    break;

                default:
                    player.SendErrorMessage(
                        "Unknown admin command. Use /worldlevel help for commands."
                    );
                    break;
            }

            SaveWorldData();
        }

        private void CheckAndResetRerolls()
        {
            if (DateTime.UtcNow >= _worldData.NextRerollReset)
            {
                // Clear all reroll data and set next reset time
                _worldData.PlayerRerolls.Clear();
                _worldData.NextRerollReset = DateTime.UtcNow.Date.AddDays(1);
                SaveWorldData();
                TShock.Log.Debug($"Daily reroll attempts reset at {DateTime.UtcNow}");

                // Broadcast reset to all players
                TShock.Utils.Broadcast("Daily task reroll attempts have been reset!", Color.Yellow);
            }
        }

        private PlayerRerollData GetOrCreatePlayerRerollData(int accountId)
        {
            if (!_worldData.PlayerRerolls.TryGetValue(accountId, out var data))
            {
                data = new PlayerRerollData { LastRerollTime = DateTime.MinValue, RerollsUsed = 0 };
                _worldData.PlayerRerolls[accountId] = data;
            }
            return data;
        }

        private async void HandleTaskReroll(TSPlayer player)
        {
            if (_worldData.CurrentTask == null)
            {
                player.SendErrorMessage("There is no active task to reroll!");
                return;
            }

            var playerData = GetOrCreatePlayerRerollData(player.Account.ID);

            // Check cooldown
            if ((DateTime.Now - playerData.LastRerollTime).TotalMinutes < REROLL_COOLDOWN_MINUTES)
            {
                var timeLeft =
                    REROLL_COOLDOWN_MINUTES
                    - (int)(DateTime.Now - playerData.LastRerollTime).TotalMinutes;
                player.SendErrorMessage(
                    $"You must wait {timeLeft} more minutes before rerolling again."
                );
                return;
            }

            // Check daily limit
            if (playerData.RerollsUsed >= DAILY_REROLL_LIMIT)
            {
                var timeUntilReset = _worldData.NextRerollReset - DateTime.UtcNow;
                player.SendErrorMessage(
                    $"You've reached your daily limit of {DAILY_REROLL_LIMIT} rerolls!"
                );
                player.SendErrorMessage(
                    $"Resets in: {timeUntilReset.Hours}h {timeUntilReset.Minutes}m"
                );
                return;
            }

            // Process payment
            var success = await _bankService.UpdateBalance(player, -REROLL_COST, "task reroll");

            if (success)
            {
                // Store the current NPC ID before clearing the task
                var oldNpcId = _worldData.CurrentTask.TargetMobId;
                _worldData.AddRecentTask(oldNpcId); // Add to recent tasks list

                _worldData.CurrentTask = null;
                _taskManager?.Update();

                playerData.LastRerollTime = DateTime.Now;
                playerData.RerollsUsed++;

                var remainingRerolls = DAILY_REROLL_LIMIT - playerData.RerollsUsed;
                player.SendSuccessMessage(
                    $"Task rerolled! {REROLL_COST} jspoints have been deducted."
                );
                player.SendMessage(
                    $"You have {remainingRerolls} rerolls remaining today.",
                    Color.Yellow
                );
                SaveWorldData();
            }
            else
            {
                player.SendErrorMessage($"You need {REROLL_COST} jspoints to reroll the task!");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SaveWorldData();

                // Deregister hooks
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNPCKill);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            }
            base.Dispose(disposing);
        }
    }
}
