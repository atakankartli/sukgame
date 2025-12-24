# Chapter 02: C# Essentials for Godot

> **Goal**: Master C# patterns specific to Godot 4 game development.

---

## 🏗️ Basic Script Structure

Every Godot C# script follows this pattern:

```csharp
using Godot;
using System;

// The 'partial' keyword is REQUIRED in Godot 4
public partial class MyNode : Node2D  // Inherits from a Godot class
{
    // ═══════════════════════════════════════════════════════════
    // SECTION 1: EXPORTS (Visible in Inspector)
    // ═══════════════════════════════════════════════════════════
    [Export] public float Speed = 100f;
    [Export] public PackedScene BulletScene;
    
    // ═══════════════════════════════════════════════════════════
    // SECTION 2: SIGNALS
    // ═══════════════════════════════════════════════════════════
    [Signal] public delegate void DiedEventHandler();
    [Signal] public delegate void HealthChangedEventHandler(float current, float max);
    
    // ═══════════════════════════════════════════════════════════
    // SECTION 3: PRIVATE FIELDS
    // ═══════════════════════════════════════════════════════════
    private AnimatedSprite2D _sprite;
    private float _health = 100f;
    
    // ═══════════════════════════════════════════════════════════
    // SECTION 4: GODOT LIFECYCLE METHODS
    // ═══════════════════════════════════════════════════════════
    public override void _Ready()
    {
        // Called once when node enters scene tree
        _sprite = GetNode<AnimatedSprite2D>("Sprite");
    }
    
    public override void _Process(double delta)
    {
        // Called every frame (for visuals, UI)
    }
    
    public override void _PhysicsProcess(double delta)
    {
        // Called every physics frame (for movement, collision)
    }
    
    // ═══════════════════════════════════════════════════════════
    // SECTION 5: PUBLIC METHODS
    // ═══════════════════════════════════════════════════════════
    public void TakeDamage(float amount)
    {
        _health -= amount;
        EmitSignal(SignalName.HealthChanged, _health, 100f);
    }
    
    // ═══════════════════════════════════════════════════════════
    // SECTION 6: PRIVATE METHODS
    // ═══════════════════════════════════════════════════════════
    private void Die()
    {
        EmitSignal(SignalName.Died);
        QueueFree();
    }
}
```

---

## 📤 Exports (Inspector Variables)

Exports expose variables to Godot's Inspector:

```csharp
// Basic types
[Export] public float Speed = 100f;
[Export] public int Health = 100;
[Export] public string Name = "Player";
[Export] public bool IsActive = true;
[Export] public Vector2 Offset = new Vector2(10, 20);
[Export] public Color TintColor = Colors.White;

// Ranges (slider in Inspector)
[Export(PropertyHint.Range, "0,100,1")]
public float Percentage = 50f;

[Export(PropertyHint.Range, "0,1,0.01")]
public float NormalizedValue = 0.5f;

// Enums (dropdown in Inspector)
[Export] public MyState CurrentState = MyState.Idle;

// Resources (drag & drop in Inspector)
[Export] public PackedScene EnemyScene;
[Export] public Texture2D Icon;
[Export] public AudioStream HitSound;

// Node paths (select node in Inspector)
[Export] public NodePath TargetPath;

// Arrays
[Export] public string[] Names;
[Export] public Texture2D[] Sprites;

// Export Groups (organize in Inspector)
[ExportGroup("Movement")]
[Export] public float MoveSpeed = 100f;
[Export] public float JumpForce = 300f;

[ExportGroup("Combat")]
[Export] public float Damage = 10f;
[Export] public float AttackSpeed = 1f;
```

---

## 📡 Signals (Events)

Signals are Godot's event system. Use them for loose coupling.

### Defining Signals

```csharp
// Simple signal (no parameters)
[Signal] public delegate void DiedEventHandler();

// Signal with parameters
[Signal] public delegate void HealthChangedEventHandler(float current, float max);
[Signal] public delegate void ItemCollectedEventHandler(string itemName, int quantity);
```

### Emitting Signals

```csharp
// Using SignalName (recommended, type-safe)
EmitSignal(SignalName.Died);
EmitSignal(SignalName.HealthChanged, _health, _maxHealth);

// Using string (works but not type-safe)
EmitSignal("Died");
```

### Connecting Signals

```csharp
// In code - using Callable
public override void _Ready()
{
    // Connect to own signal
    Died += OnDied;
    
    // Connect to child node's signal
    var button = GetNode<Button>("Button");
    button.Pressed += OnButtonPressed;
    
    // Connect to signal with parameters
    HealthChanged += OnHealthChanged;
}

// Handler methods
private void OnDied()
{
    GD.Print("I died!");
}

private void OnHealthChanged(float current, float max)
{
    GD.Print($"Health: {current}/{max}");
}

private void OnButtonPressed()
{
    GD.Print("Button clicked!");
}
```

### Disconnecting Signals

```csharp
// Important when nodes are freed
public override void _ExitTree()
{
    Died -= OnDied;
    HealthChanged -= OnHealthChanged;
}
```

---

## 🔍 Getting Nodes

### GetNode<T>() - Must Exist

```csharp
// Crashes if node doesn't exist (use when you're SURE it exists)
_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

// Relative paths
var parent = GetNode<Node2D>("..");           // Parent
var sibling = GetNode<Node2D>("../Sibling");  // Sibling
var deep = GetNode<Node2D>("Child/GrandChild");
```

### GetNodeOrNull<T>() - Might Not Exist

```csharp
// Returns null if not found (safe)
var optional = GetNodeOrNull<Label>("OptionalLabel");
if (optional != null)
{
    optional.Text = "Found!";
}
```

### Finding Nodes

```csharp
// By group (set groups in Inspector or code)
var enemies = GetTree().GetNodesInGroup("enemies");

// First in group
var player = GetTree().GetFirstNodeInGroup("player") as Player;

// By type in children
var allSprites = GetChildren().OfType<Sprite2D>();

// Find in entire scene
var hud = GetTree().CurrentScene.FindChild("HUD", true, false) as CanvasLayer;
```

---

## ⏱️ Lifecycle Methods

```csharp
// Called when node is created (before _Ready)
public override void _EnterTree()
{
    // Node is in the tree but children might not be ready
}

// Called when node and ALL children are ready
public override void _Ready()
{
    // Safe to access children here
    // Called ONCE when first entering tree
}

// Called every frame (~60 times/sec at 60fps)
// Use for: visuals, UI updates, non-physics logic
public override void _Process(double delta)
{
    float dt = (float)delta;  // Cast to float for convenience
    _animationTime += dt;
}

// Called every physics frame (fixed timestep, default 60/sec)
// Use for: movement, physics, collision detection
public override void _PhysicsProcess(double delta)
{
    float dt = (float)delta;
    Velocity += _gravity * dt;
    MoveAndSlide();
}

// Called for unhandled input
public override void _UnhandledInput(InputEvent @event)
{
    if (@event.IsActionPressed("pause"))
    {
        GetTree().Paused = !GetTree().Paused;
    }
}

// Called when node is about to be removed from tree
public override void _ExitTree()
{
    // Clean up: disconnect signals, free resources
}
```

---

## 🎬 Tweens (Animation in Code)

Tweens smoothly animate properties:

```csharp
// Basic tween
var tween = CreateTween();
tween.TweenProperty(this, "position", new Vector2(100, 100), 0.5f);

// Chained animations (sequential)
var tween = CreateTween();
tween.TweenProperty(this, "position:x", 200f, 0.3f);  // First
tween.TweenProperty(this, "position:y", 150f, 0.3f);  // Then

// Parallel animations
var tween = CreateTween();
tween.TweenProperty(this, "position", targetPos, 0.5f);
tween.Parallel().TweenProperty(this, "modulate:a", 0f, 0.5f);  // Same time

// Easing (feel)
var tween = CreateTween();
tween.SetEase(Tween.EaseType.Out);           // Slow at end
tween.SetTrans(Tween.TransitionType.Cubic);  // Smooth curve
tween.TweenProperty(this, "scale", Vector2.One * 2, 0.3f);

// Callback when done
tween.TweenCallback(Callable.From(() => GD.Print("Done!")));

// Loop
var tween = CreateTween().SetLoops();  // Infinite loops
tween.TweenProperty(sprite, "modulate:a", 0.5f, 0.5f);
tween.TweenProperty(sprite, "modulate:a", 1f, 0.5f);

// Kill existing tween before creating new
_activeTween?.Kill();
_activeTween = CreateTween();
```

### Common Easing Types

```
EaseType.In    = Starts slow, speeds up
EaseType.Out   = Starts fast, slows down (most natural)
EaseType.InOut = Slow at both ends

TransitionType.Linear  = Constant speed (robotic)
TransitionType.Quad    = Smooth
TransitionType.Cubic   = Smoother
TransitionType.Elastic = Bouncy/springy
TransitionType.Bounce  = Bounces at end
TransitionType.Back    = Overshoots slightly
```

---

## ⏰ Timers

### Timer Node (in scene tree)

```csharp
// Get existing timer
var timer = GetNode<Timer>("AttackCooldown");
timer.Timeout += OnAttackReady;
timer.Start();

// Check if running
if (!timer.IsStopped())
{
    GD.Print($"Time left: {timer.TimeLeft}");
}
```

### SceneTreeTimer (one-shot, no node needed)

```csharp
// Wait for duration, then call method
GetTree().CreateTimer(2.0).Timeout += () => {
    GD.Print("2 seconds passed!");
};

// Async/await style
await ToSignal(GetTree().CreateTimer(1.5), "timeout");
GD.Print("1.5 seconds passed!");
```

---

## 📦 Resources (Data Containers)

Resources are data files that can be edited in Inspector:

```csharp
// Define a resource class
using Godot;

[GlobalClass]  // Makes it appear in "Create New Resource" menu
public partial class WeaponData : Resource
{
    [Export] public string Name = "Weapon";
    [Export] public float Damage = 10f;
    [Export] public float AttackSpeed = 1f;
    [Export] public Texture2D Icon;
    [Export] public AudioStream AttackSound;
}
```

```csharp
// Use in another script
public partial class Player : CharacterBody2D
{
    [Export] public WeaponData EquippedWeapon;
    
    public void Attack()
    {
        if (EquippedWeapon == null) return;
        
        DealDamage(EquippedWeapon.Damage);
        PlaySound(EquippedWeapon.AttackSound);
    }
}
```

### Creating Resource Files
1. Right-click in FileSystem → Create New → Resource
2. Select your resource type (e.g., WeaponData)
3. Fill in values in Inspector
4. Save as .tres file

---

## 🔧 Common Patterns

### Null-Conditional Operators

```csharp
// Safe navigation
_sprite?.Play("idle");           // Only calls if not null
var name = _weapon?.Name ?? "Unarmed";  // Default if null

// Null coalescing assignment
_sprite ??= GetNode<Sprite2D>("Sprite");  // Assign if null
```

### Caching Node References

```csharp
// BAD: Gets node every frame
public override void _Process(double delta)
{
    GetNode<Label>("UI/Health").Text = $"{_health}";  // Slow!
}

// GOOD: Cache in _Ready, use cached reference
private Label _healthLabel;

public override void _Ready()
{
    _healthLabel = GetNode<Label>("UI/Health");
}

public override void _Process(double delta)
{
    _healthLabel.Text = $"{_health}";  // Fast!
}
```

### Extension Methods

```csharp
// In Utils/NodeExtensions.cs
public static class NodeExtensions
{
    public static T GetNodeOrWarn<T>(this Node node, string path) where T : class
    {
        var result = node.GetNodeOrNull<T>(path);
        if (result == null)
            GD.PrintErr($"{node.Name}: Could not find {typeof(T).Name} at '{path}'");
        return result;
    }
}

// Usage
_sprite = this.GetNodeOrWarn<AnimatedSprite2D>("Sprite");
```

---

## ⚠️ Common Mistakes

### Mistake 1: Forgetting `partial`
```csharp
// WRONG - won't compile in Godot 4
public class Player : CharacterBody2D

// CORRECT
public partial class Player : CharacterBody2D
```

### Mistake 2: Wrong delta type
```csharp
// delta is double, but most Godot methods want float
public override void _PhysicsProcess(double delta)
{
    // Cast it
    float dt = (float)delta;
    Position += Velocity * dt;
}
```

### Mistake 3: Signal naming
```csharp
// WRONG - doesn't generate SignalName
[Signal] public delegate void OnDied();

// CORRECT - must end with EventHandler
[Signal] public delegate void DiedEventHandler();
```

### Mistake 4: Accessing freed nodes
```csharp
// WRONG - node might be freed
_enemy.TakeDamage(10);

// CORRECT - check validity
if (IsInstanceValid(_enemy))
{
    _enemy.TakeDamage(10);
}
```

---

**Next Chapter**: [03 - Node System](03_NODE_SYSTEM.md)

