# WorldLevel - Terraria TShock Plugin

A progression-based world leveling system for Terraria servers using TShock. This plugin adds RPG-like elements to your server by introducing tasks, world levels, and boss progression locks.

## Features

- **World Leveling System**
  - Gain XP through completing tasks
  - Progressive difficulty scaling
  - Biome-specific challenges
  - Dynamic task generation

- **Boss Progression**
  - Bosses are locked behind world level requirements
  - Natural progression from pre-hardmode to hardmode
  - Prevents boss spawning until appropriate level
  - Level-based boss availability

- **Task System**
  - Biome-based enemy tasks
  - Scaling rewards based on difficulty
  - Dynamic goal calculation
  - Progress tracking and announcements

- **Biome Integration**
  - Surface/Forest tasks
  - Underground challenges
  - Corruption/Crimson missions
  - Dungeon adventures
  - Underworld trials
  - Hardmode biome challenges

## Progression System

### Pre-Hardmode Bosses & Level Requirements
1. King Slime (Level 1)
2. Eye of Cthulhu (Level 1)
3. Eater of Worlds/Brain of Cthulhu (Level 2)
4. Queen Bee (Level 3)
5. Deerclops (Level 3)
6. Skeletron (Level 4)
7. Wall of Flesh (Level 5)

### Hardmode Bosses & Level Requirements
8. Queen Slime (Level 6)
9. The Destroyer (Level 7)
10. The Twins (Level 7)
11. Skeletron Prime (Level 7)
12. Plantera (Level 8)
13. Golem (Level 9)
14. Duke Fishron (Level 10)
15. Empress of Light (Level 10)
16. Lunatic Cultist (Level 11)
17. Moon Lord (Level 12)

## Installation

1. Download the latest release from the releases page
2. Place the `WorldLevel.dll` file in your server's `ServerPlugins` folder
3. Restart your TShock server

## Commands

### General Commands
- `/worldlevel` or `/wl` - Show basic world level information
- `/wl help` - Display list of available commands
- `/wl status` - Show detailed world progress including:
  - Current world level
  - XP progress
  - Distance to next level
  - Available bosses

- `/wl task` - Display current task details including:
  - Target enemy type
  - Progress (kills/total)
  - XP reward
  - Associated boss

### Admin Commands
Requires `worldlevel.admin` permission:
- `/wl admin setlevel <level>` - Set the world's current level
- `/wl admin addxp <amount>` - Add XP to the world's total
- `/wl admin newtask` - Force generate a new task

### Permissions
- `worldlevel` - Basic command access (default: true)
- `worldlevel.admin` - Admin command access (default: false)

## Configuration

The plugin creates a `worldlevel.json` file in your TShock settings folder:

```json
{
  "WorldLevel": 0,
  "CurrentXP": 0,
  "RequiredXP": 10240,
  "CurrentTask": null
}
```

### XP Scaling
- Base XP: 10,240
- Level Multiplier: 3.8x
- Task rewards scale with level difficulty

## Development

Built with:
- .NET Framework 4.7.2
- TShock API 2.1
- Terraria Server API

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- TShock team for their amazing server mod
- Terraria development team
- Community contributors and testers