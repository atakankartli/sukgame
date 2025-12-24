# Chapter 03: Node System & Scene Composition

> **Goal**: Understand Godot's node-based architecture and build reusable scenes.

---

## 🌳 The Node Tree

Everything in Godot is a **Node**. Nodes form a tree:

```
Main (Node2D)                    ← Root of scene
├── Player (CharacterBody2D)     ← Child
│   ├── Sprite (Sprite2D)        ← Grandchild
│   ├── Collision (CollisionShape2D)
│   └── Camera (Camera2D)
├── Enemies (Node2D)             ← Container for enemies
│   ├── Goblin (CharacterBody2D)
│   └── Skeleton (CharacterBody2D)
└── UI (CanvasLayer)
    └── HUD (Control)
```

### Key Concepts:
- **Parent/Child**: Children move with parent, inherit transforms
- **Processing Order**: Parent processes before children
- **Scene = Branch**: Any node with children can be saved as a scene

---

## 📦 Common Node Types

### 2D Nodes
| Node | Use For |
|------|---------|
| `Node2D` | Base 2D node, containers |
| `Sprite2D` | Static images |
| `AnimatedSprite2D` | Sprite animations |
| `CharacterBody2D` | Player, enemies (you control movement) |
| `RigidBody2D` | Physics objects (physics controls movement) |
| `StaticBody2D` | Walls, floors (doesn't move) |
| `Area2D` | Triggers, hitboxes, pickups |
| `TileMap` | Tile-based levels |
| `Camera2D` | Game camera |

### UI Nodes
| Node | Use For |
|------|---------|
| `CanvasLayer` | UI container (separate from game) |
| `Control` | Base UI node |
| `Label` | Text display |
| `Button` | Clickable button |
| `TextureRect` | UI images |
| `ProgressBar` | Health bars, loading |
| `Container` | Layout containers (HBox, VBox, Grid) |

### Utility Nodes
| Node | Use For |
|------|---------|
| `Node` | Pure logic, no transform |
| `Timer` | Delayed/repeated callbacks |
| `AudioStreamPlayer2D` | Positional audio |
| `AnimationPlayer` | Complex animations |
| `GPUParticles2D` | Particle effects |

---

## 🎬 Scenes

A **Scene** is a saved node tree that can be instantiated multiple times.

### Creating Scenes

1. Build your node tree
2. Select the root node
3. Scene → Save Scene (or Ctrl+S)
4. Save as `.tscn` file

### Scene Structure Best Practices

```
# Player Scene (player.tscn)
Player (CharacterBody2D)        ← Root has the main script
├── AnimatedSprite2D            ← Visuals
├── CollisionShape2D            ← Physics collision
├── Hurtbox (Area2D)            ← Damage detection
│   └── CollisionShape2D
├── CombatStats (Node)          ← Health component
├── MeleeWeapon (Node2D)        ← Weapon system
│   └── AttackHitbox (Area2D)
└── Camera2D                    ← Follows player

# Enemy Scene (enemy.tscn)
Enemy (CharacterBody2D)
├── AnimatedSprite2D
├── CollisionShape2D
├── Hurtbox (Area2D)
│   └── CollisionShape2D
├── CombatStats (Node)
├── HealthBar (Node2D)          ← Visual HP bar
└── DetectionArea (Area2D)      ← Player detection
    └── CollisionShape2D
```

---

## 🔄 Instancing Scenes

### In Editor
1. Right-click in Scene tree
2. "Instantiate Child Scene"
3. Select your `.tscn` file

### In Code

```csharp
// Load and instantiate
[Export] public PackedScene EnemyScene;  // Drag scene to Inspector

public void SpawnEnemy(Vector2 position)
{
    var enemy = EnemyScene.Instantiate<Enemy>();
    GetTree().CurrentScene.AddChild(enemy);
    enemy.GlobalPosition = position;
}

// Or load at runtime
public void SpawnFromPath()
{
    var scene = GD.Load<PackedScene>("res://scenes/enemies/goblin.tscn");
    var enemy = scene.Instantiate<Enemy>();
    AddChild(enemy);
}
```

---

## 🔗 Scene Composition

Build complex objects from simple scenes:

```
# Reusable HealthBar scene
HealthBar (Node2D)
├── Background (ColorRect)
├── Fill (ColorRect)
└── HealthBar.cs

# Use in Player
Player (CharacterBody2D)
├── ... other nodes ...
└── HealthBar (instance of HealthBar.tscn)

# Use in Enemy
Enemy (CharacterBody2D)
├── ... other nodes ...
└── HealthBar (instance of HealthBar.tscn)
```

### Scene Inheritance

Create a base scene, then extend it:

1. Create `base_enemy.tscn` with common structure
2. Create new scene → "New Inherited Scene"
3. Select `base_enemy.tscn`
4. Customize for specific enemy
5. Save as `goblin.tscn`

---

## 👥 Groups

Groups are tags for nodes. Any node can be in multiple groups.

### Setting Groups (Editor)
1. Select node
2. Node panel (next to Inspector) → Groups tab
3. Add group name

### Setting Groups (Code)
```csharp
public override void _Ready()
{
    AddToGroup("enemies");
    AddToGroup("damageable");
}
```

### Using Groups
```csharp
// Find all nodes in group
var enemies = GetTree().GetNodesInGroup("enemies");
foreach (var enemy in enemies)
{
    (enemy as Enemy)?.TakeDamage(10);
}

// Find first in group
var player = GetTree().GetFirstNodeInGroup("player") as Player;

// Check if in group
if (IsInGroup("enemies"))
{
    // This is an enemy
}

// Remove from group
RemoveFromGroup("stunned");
```

### Common Groups
```
player          - The player character
enemies         - All enemy instances
projectiles     - Bullets, arrows, etc.
damageable      - Anything that can take damage
interactable    - Objects player can interact with
persistent      - Don't destroy on scene change
```

---

## 🔍 Finding Nodes

### Direct Path (Fastest)
```csharp
// Absolute path from scene root
var player = GetNode<Player>("/root/Main/Player");

// Relative to current node
var sprite = GetNode<Sprite2D>("Sprite");           // Child
var parent = GetNode<Node2D>("..");                 // Parent
var sibling = GetNode<Node2D>("../Sibling");        // Sibling
var deep = GetNode<Node2D>("Child/Grandchild");     // Nested
```

### Safe Access
```csharp
var optional = GetNodeOrNull<Label>("MaybeLabel");
if (optional != null)
{
    optional.Text = "Found!";
}
```

### By Group
```csharp
var player = GetTree().GetFirstNodeInGroup("player") as Player;
```

### By Type (All Children)
```csharp
// Using LINQ
var allSprites = GetChildren().OfType<Sprite2D>();

// Recursive search
var button = FindChild("StartButton") as Button;
```

---

## ⏰ Lifecycle Events

```csharp
// Called when node enters the tree
public override void _EnterTree()
{
    GD.Print("Entered tree");
}

// Called when node AND all children are ready
public override void _Ready()
{
    GD.Print("Ready!");  // Safe to access children here
}

// Called every frame
public override void _Process(double delta)
{
    // Update logic
}

// Called every physics frame (fixed timestep)
public override void _PhysicsProcess(double delta)
{
    // Physics logic
}

// Called when node exits the tree
public override void _ExitTree()
{
    GD.Print("Exiting tree");  // Clean up here
}
```

### Order of Operations
1. `_EnterTree()` - Top to bottom
2. `_Ready()` - Bottom to top (children ready before parents)
3. `_Process/_PhysicsProcess` - Top to bottom every frame
4. `_ExitTree()` - Bottom to top

---

## 🗑️ Freeing Nodes

```csharp
// Queue for deletion (safe, waits until end of frame)
QueueFree();

// Immediate deletion (careful with references!)
node.Free();

// Remove from tree but don't delete
RemoveChild(node);

// Check if node is being deleted
if (IsQueuedForDeletion())
    return;

// Check if reference is still valid
if (IsInstanceValid(someNode))
{
    someNode.DoSomething();
}
```

---

## 📋 Scene Design Checklist

- [ ] Root node has the main script
- [ ] Logical hierarchy (visuals, collision, components)
- [ ] Reusable parts are separate scenes
- [ ] Groups assigned for finding nodes
- [ ] No hardcoded paths to other scenes' internals
- [ ] Components communicate via signals

---

**Next Chapter**: [04 - Input System](04_INPUT_SYSTEM.md)

