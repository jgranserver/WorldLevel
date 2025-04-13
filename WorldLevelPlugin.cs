using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using WorldLevel.Models;

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

        public override string Name => "World Level";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "jgranserver";
        public override string Description => "A world leveling system with tasks and boss unlocks";

        public WorldLevelPlugin(Main game)
            : base(game)
        {
            _worldData = new WorldData();
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
                    _worldData = new WorldData();
                    SaveWorldData(); // Create initial file
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Failed to load world data: {ex.Message}");
                _worldData = new WorldData();
            }
        }

        private void SaveWorldData()
        {
            File.WriteAllText(SavePath, JsonSerializer.Serialize(_worldData));
        }

        private void OnGameUpdate(EventArgs args)
        {
            _taskManager?.Update();
        }

        private void OnNPCKill(NpcKilledEventArgs args)
        {
            try
            {
                if (args.npc?.active != true || args.npc.friendly || args.npc.townNPC)
                    return;

                var npcId = args.npc.netID;
                TShock.Log.Debug($"NPC Killed - ID: {npcId}, Name: {Lang.GetNPCNameValue(npcId)}");

                // Check if NPC matches current task
                if (_worldData.CurrentTask?.TargetMobId == npcId)
                {
                    bool canBroadcast =
                        (DateTime.Now - _lastTaskBroadcast).TotalSeconds
                        >= BROADCAST_COOLDOWN_SECONDS;

                    var player = TShock.Players.FirstOrDefault(p =>
                        p?.Active == true && p.Index == args.npc.target
                    );

                    _taskManager?.HandleNpcKill(npcId, player, canBroadcast);

                    if (canBroadcast)
                    {
                        _lastTaskBroadcast = DateTime.Now;
                    }

                    SaveWorldData();
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Error in OnNPCKill: {ex.Message}");
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

                // Handle only positive values (bosses) for now
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

            if (player.HasPermission("worldlevel.admin"))
            {
                player.SendMessage("╠══════ Admin Commands ══════╣", Color.Orange);
                player.SendMessage("║ /wl admin setlevel <level>", Color.White);
                player.SendMessage("║ /wl admin addxp <amount>", Color.White);
                player.SendMessage("║ /wl admin newtask", Color.White);
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

                default:
                    player.SendErrorMessage(
                        "Unknown admin command. Use /worldlevel help for commands."
                    );
                    break;
            }

            SaveWorldData();
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
