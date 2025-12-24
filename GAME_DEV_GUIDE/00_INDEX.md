# 🎮 Godot 4 C# Game Development Guide
## Isometric 2D Action Game Reference

---

## 📚 Table of Contents

### Part 1: Foundations
| Chapter | Topic | Description |
|---------|-------|-------------|
| [01](01_PROJECT_SETUP.md) | **Project Setup** | Folder structure, settings, .csproj configuration |
| [02](02_CSHARP_ESSENTIALS.md) | **C# Essentials** | Godot-specific C# patterns, exports, signals |
| [03](03_NODE_SYSTEM.md) | **Node System** | Scene tree, common nodes, scene composition |
| [04](04_INPUT_SYSTEM.md) | **Input System** | Input mapping, buffering, device detection |

### Part 2: Core Systems
| Chapter | Topic | Description |
|---------|-------|-------------|
| [05](05_MOVEMENT_PHYSICS.md) | **Movement & Physics** | CharacterBody2D, collision, smooth movement |
| [06](06_ANIMATION_SYSTEM.md) | **Animation System** | AnimatedSprite2D, AnimationPlayer, state-based anims |
| [07](07_STATE_MACHINES.md) | **State Machines** | Player states, enemy states, clean transitions |
| [08](08_COMBAT_SYSTEM.md) | **Combat System** | Hitboxes, hurtboxes, damage, melee attacks |

### Part 3: Game Systems
| Chapter | Topic | Description |
|---------|-------|-------------|
| [09](09_ENEMY_AI.md) | **Enemy AI** | Detection, pathfinding, attack patterns |
| [10](10_UI_HUD.md) | **UI & HUD** | Health bars, menus, responsive layouts |
| [11](11_AUDIO.md) | **Audio** | SFX, music, audio buses, positional audio |
| [12](12_SAVE_SYSTEM.md) | **Save System** | JSON saves, settings, multiple slots |

### Part 4: Polish & Production
| Chapter | Topic | Description |
|---------|-------|-------------|
| [13](13_SCENE_MANAGEMENT.md) | **Scene Management** | Level transitions, loading screens, game flow |
| [14](14_OPTIMIZATION.md) | **Optimization** | Profiling, pooling, performance tips |

### Part 5: Specialized Topics
| Chapter | Topic | Description |
|---------|-------|-------------|
| [15](15_ISOMETRIC_TIPS.md) | **Isometric Tips** | Y-sorting, tilemap setup, diagonal movement |
| [16](16_COMMON_PATTERNS.md) | **Common Patterns** | Singletons, resources, composition |

### Appendices
| Appendix | Topic | Description |
|----------|-------|-------------|
| [A](A_CHEAT_SHEETS.md) | **Cheat Sheets** | Quick reference for common tasks |

---

## 🚀 Quick Start

1. **New to Godot?** Start with chapters 01-04
2. **Building gameplay?** Focus on 05-08
3. **Adding enemies?** See chapter 09
4. **Polish phase?** Check 13-14
5. **Quick lookup?** Jump to Appendix A

---

## 🎯 This Guide Covers

- ✅ Godot 4.x with C#
- ✅ 2D isometric games
- ✅ Action/combat gameplay (Souls-like inspired)
- ✅ Scalable architecture
- ✅ Practical, copy-paste code

---

## 📖 How to Use This Guide

Each chapter follows this format:

1. **Goal** - What you'll learn
2. **Concepts** - Theory and explanations
3. **Code** - Practical implementations
4. **Checklist** - Summary of key points

Code is designed to be:
- **Modular** - Each system works independently
- **Extensible** - Easy to add features
- **Clear** - Comments explain the "why"

---

## 🔗 Related Files in Your Project

```
sukgame/
├── Scripts/
│   ├── Player.cs              → Ch 05, 07, 08
│   ├── Enemy.cs               → Ch 09
│   ├── Combat/
│   │   ├── CombatStats.cs     → Ch 08
│   │   ├── Hitbox.cs          → Ch 08
│   │   ├── Hurtbox.cs         → Ch 08
│   │   └── Weapons/
│   │       ├── Weapon.cs      → Ch 08
│   │       └── MeleeWeapon.cs → Ch 08
│   └── Skills/                → Ch 02 (Resources)
├── scenes/
│   ├── player.tscn            → Ch 03
│   └── enemy.tscn             → Ch 03
└── project.godot              → Ch 01
```

---

**Happy Game Dev! 🎮**
