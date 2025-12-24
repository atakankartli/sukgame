# Chapter 01: Project Setup & Structure

> **Goal**: Set up a clean, scalable project structure that grows with your game.

---

## 📁 Folder Structure

```
YourGame/
├── .godot/                 # Godot internal (auto-generated, gitignore)
├── assets/                 # All raw art, audio, fonts
│   ├── sprites/
│   │   ├── player/
│   │   ├── enemies/
│   │   ├── effects/
│   │   └── ui/
│   ├── audio/
│   │   ├── sfx/
│   │   └── music/
│   ├── fonts/
│   └── shaders/
├── scenes/                 # .tscn scene files
│   ├── characters/
│   │   ├── player.tscn
│   │   └── enemies/
│   ├── levels/
│   ├── ui/
│   └── prefabs/            # Reusable scene pieces
├── scripts/                # .cs script files
│   ├── Core/               # Singletons, managers
│   ├── Player/
│   ├── Enemies/
│   ├── Combat/
│   ├── UI/
│   └── Utils/              # Helper classes
├── resources/              # .tres resource files
│   ├── weapons/
│   ├── items/
│   └── effects/
├── addons/                 # Third-party plugins
├── project.godot
├── .gitignore
└── README.md
```

### Why This Structure?

1. **assets/** vs **scenes/** - Separates raw files from Godot scenes
2. **scripts/** mirrors **scenes/** - Easy to find related code
3. **resources/** - Data-driven design with .tres files
4. **prefabs/** - Reusable scene pieces (like Unity prefabs)

---

## 📛 Naming Conventions

### Files & Folders
```
folders:        snake_case      (player_abilities/)
scenes:         snake_case.tscn (player.tscn, enemy_goblin.tscn)
scripts:        PascalCase.cs   (Player.cs, EnemyGoblin.cs)
resources:      PascalCase.tres (IronSword.tres, FireEffect.tres)
sprites:        snake_case.png  (player_idle.png, player_walk_01.png)
```

### In Code
```csharp
// Classes: PascalCase
public partial class PlayerController : CharacterBody2D

// Methods: PascalCase
public void TakeDamage(float amount)

// Private fields: _camelCase with underscore
private float _health;
private AnimatedSprite2D _sprite;

// Public properties: PascalCase
public float Health { get; private set; }

// Constants: UPPER_SNAKE_CASE
private const float MAX_SPEED = 200f;

// Signals: PascalCase with EventHandler suffix
[Signal] public delegate void HealthChangedEventHandler(float current, float max);

// Enums: PascalCase
public enum PlayerState { Idle, Walking, Attacking, Dead }
```

---

## ⚙️ Project Settings

### Essential Settings (Project → Project Settings)

#### Display
```
Window/Size/Viewport Width: 320   (or your base resolution)
Window/Size/Viewport Height: 180
Window/Stretch/Mode: canvas_items (pixel-perfect scaling)
Window/Stretch/Aspect: keep       (maintain aspect ratio)
```

#### Rendering (for pixel art)
```
Textures/Canvas Textures/Default Texture Filter: Nearest
```

#### Physics
```
2D/Default Gravity: 0 (for top-down games)
Common/Physics Ticks Per Second: 60
```

---

## 🔢 Collision Layers

Set up meaningful layer names (Project → Project Settings → General → Layer Names → 2D Physics):

```
Layer 1: player
Layer 2: enemy
Layer 3: hurtbox        (receives damage)
Layer 4: hitbox         (deals damage)
Layer 5: projectile
Layer 6: pickup
Layer 7: obstacle
Layer 8: trigger        (areas that trigger events)
```

### Collision Matrix Cheat Sheet:
```
                  player  enemy  hurtbox  hitbox  projectile
player              -       ✓       -        -         -
enemy               ✓       -       -        -         -
hurtbox             -       -       -        ✓         ✓
hitbox              -       -       ✓        -         -
projectile          -       -       ✓        -         -
```

---

## 🎹 Input Map

Create meaningful action names (Project → Project Settings → Input Map):

```
# Movement
move_left       → A, Left Arrow, Gamepad Left
move_right      → D, Right Arrow, Gamepad Right
move_up         → W, Up Arrow, Gamepad Up
move_down       → S, Down Arrow, Gamepad Down

# Combat
attack          → Left Mouse, Gamepad X
skill1          → 1, Gamepad Y
skill2          → 2, Gamepad B
dodge           → Space, Shift, Gamepad A
block           → Right Mouse, Gamepad LT

# UI
pause           → Escape, Gamepad Start
inventory       → Tab, I, Gamepad Select
interact        → E, F, Gamepad A
```

---

## 📝 .gitignore

Create `.gitignore` in project root:

```gitignore
# Godot 4 specific
.godot/

# Mono/C# specific
.mono/
*.csproj
*.sln

# But keep these if you need IDE support:
# !*.csproj
# !*.sln

# OS files
.DS_Store
Thumbs.db

# IDE
.idea/
.vscode/
*.code-workspace

# Build outputs
build/
export/
*.pck
*.zip

# Backup files
*~
*.backup
```

---

## 🚀 First Steps After Setup

### 1. Create Autoload Singletons
(Scene → Project Settings → Autoload)

```
GameManager     → res://scripts/Core/GameManager.cs
AudioManager    → res://scripts/Core/AudioManager.cs
EventBus        → res://scripts/Core/EventBus.cs
```

### 2. Create Base Scene
Create a main scene (`main.tscn`) that contains:
```
Main (Node2D)
├── World (Node2D)          ← Level content goes here
├── UI (CanvasLayer)        ← HUD goes here
└── PauseMenu (CanvasLayer) ← Pause menu
```

### 3. Set Main Scene
Project → Project Settings → Application → Run → Main Scene

---

## 📋 Pre-Flight Checklist

Before coding, verify:

- [ ] Folder structure created
- [ ] Collision layers named
- [ ] Input actions defined
- [ ] Project settings configured (resolution, stretch mode)
- [ ] .gitignore in place
- [ ] Main scene created
- [ ] Git initialized (`git init`)

---

## 💡 Pro Tips

### Tip 1: Use Resource UIDs
Godot 4 uses UIDs for resources. If you move/rename files, references stay intact.
Check .uid files are committed to Git.

### Tip 2: Create a Test Scene
Have a `debug.tscn` scene for testing features in isolation.

### Tip 3: Version Your Saves
Put save format version in save files so you can migrate old saves.

### Tip 4: Screenshot Key
Add a debug action to take screenshots:
```csharp
if (Input.IsActionJustPressed("debug_screenshot"))
{
    var image = GetViewport().GetTexture().GetImage();
    image.SavePng($"user://screenshot_{Time.GetTicksMsec()}.png");
}
```

---

**Next Chapter**: [02 - C# Essentials](02_CSHARP_ESSENTIALS.md)

