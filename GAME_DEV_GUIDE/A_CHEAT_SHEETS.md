# Appendix A: Cheat Sheets

> **Quick reference for common code patterns. Copy-paste ready!**

---

## 🎮 Input

```csharp
// Check if action was just pressed this frame
if (Input.IsActionJustPressed("attack"))

// Check if action is being held
if (Input.IsActionPressed("block"))

// Check if action was just released
if (Input.IsActionJustReleased("jump"))

// Get movement as Vector2 (already normalized)
Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");

// Get mouse position in world coordinates
Vector2 mousePos = GetGlobalMousePosition();

// Get direction to mouse
Vector2 mouseDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();

// Check any key pressed
if (@event is InputEventKey keyEvent && keyEvent.Pressed)
```

---

## 🏃 Movement

```csharp
// Basic movement (CharacterBody2D)
public override void _PhysicsProcess(double delta)
{
    Vector2 input = Input.GetVector("left", "right", "up", "down");
    Velocity = input * Speed;
    MoveAndSlide();
}

// Smooth acceleration/deceleration
Velocity = Velocity.MoveToward(targetVelocity, Acceleration * (float)delta);

// Lerp (smooth interpolation)
Position = Position.Lerp(targetPosition, 0.1f);

// Move toward position
Position = Position.MoveToward(targetPosition, Speed * (float)delta);

// Look at target
Rotation = (target.GlobalPosition - GlobalPosition).Angle();

// Distance check
float distance = GlobalPosition.DistanceTo(target.GlobalPosition);
if (distance < AttackRange)
```

---

## 🎬 Animation

```csharp
// Play animation
_sprite.Play("walk");

// Play animation from beginning
_sprite.Play("attack");
_sprite.Frame = 0;

// Check current animation
if (_sprite.Animation == "idle")

// Check if animation exists
if (_sprite.SpriteFrames.HasAnimation("die"))

// Connect to animation finished
_sprite.AnimationFinished += OnAnimationFinished;

// Animation speed
_sprite.SpeedScale = 2.0f;  // Double speed

// Flip horizontally
_sprite.FlipH = direction.X < 0;
```

---

## ⏱️ Timers & Delays

```csharp
// One-shot timer (no node needed)
await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
GD.Print("1.5 seconds passed!");

// Timer with callback
GetTree().CreateTimer(2.0f).Timeout += () => {
    GD.Print("Timer done!");
};

// Check Timer node
if (_attackCooldownTimer.IsStopped())
{
    _attackCooldownTimer.Start();
}

// Time remaining
float timeLeft = _timer.TimeLeft;
```

---

## 🎭 Tweens

```csharp
// Basic tween
var tween = CreateTween();
tween.TweenProperty(this, "position", targetPos, 0.5f);

// With easing
tween.SetEase(Tween.EaseType.Out);
tween.SetTrans(Tween.TransitionType.Cubic);

// Parallel (simultaneous)
tween.TweenProperty(this, "position", pos, 0.5f);
tween.Parallel().TweenProperty(this, "modulate:a", 0f, 0.5f);

// Sequential (one after another)
tween.TweenProperty(this, "position:x", 100f, 0.3f);
tween.TweenProperty(this, "position:y", 100f, 0.3f);

// Callback when done
tween.TweenCallback(Callable.From(() => QueueFree()));

// Loop
var tween = CreateTween().SetLoops();  // Infinite
var tween = CreateTween().SetLoops(3); // 3 times

// Kill existing tween
_currentTween?.Kill();
_currentTween = CreateTween();

// Delay
tween.TweenInterval(0.5f);  // Wait 0.5s before next tween
```

---

## 📡 Signals

```csharp
// Define signal
[Signal] public delegate void DiedEventHandler();
[Signal] public delegate void HealthChangedEventHandler(float current, float max);

// Emit signal
EmitSignal(SignalName.Died);
EmitSignal(SignalName.HealthChanged, _health, _maxHealth);

// Connect in code
healthComponent.Died += OnDied;
button.Pressed += OnButtonPressed;

// Disconnect
healthComponent.Died -= OnDied;

// C# event (for complex types)
public event Action<DamageInfo> DamageTaken;
DamageTaken?.Invoke(damageInfo);
```

---

## 🔍 Node Access

```csharp
// Get child node (crashes if not found)
var sprite = GetNode<AnimatedSprite2D>("Sprite");

// Get child node (returns null if not found)
var optional = GetNodeOrNull<Label>("OptionalLabel");

// Get parent
var parent = GetParent<Node2D>();

// Get sibling
var sibling = GetParent().GetNode<Node2D>("Sibling");

// Find in group
var player = GetTree().GetFirstNodeInGroup("player") as Player;
var enemies = GetTree().GetNodesInGroup("enemies");

// Check if node is valid (not freed)
if (IsInstanceValid(targetNode))
```

---

## 🎲 Random

```csharp
// Random float between 0 and 1
float rand = GD.Randf();

// Random float in range
float rand = (float)GD.RandRange(10.0, 20.0);

// Random int in range (inclusive)
int rand = GD.RandRange(0, 5);  // 0, 1, 2, 3, 4, or 5

// Random item from array
var item = items[GD.RandRange(0, items.Length - 1)];

// Chance check (30% chance)
if (GD.Randf() < 0.3f)

// Random direction
Vector2 randomDir = new Vector2(GD.Randf() - 0.5f, GD.Randf() - 0.5f).Normalized();

// Shuffle array
var shuffled = items.OrderBy(x => GD.Randf()).ToArray();
```

---

## 🎨 Visual Effects

```csharp
// Flash white
_sprite.Modulate = Colors.White * 1.5f;
await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
_sprite.Modulate = Colors.White;

// Fade out and delete
var tween = CreateTween();
tween.TweenProperty(this, "modulate:a", 0f, 0.5f);
tween.TweenCallback(Callable.From(() => QueueFree()));

// Screen shake
var camera = GetViewport().GetCamera2D();
var offset = camera.Offset;
camera.Offset = offset + new Vector2(GD.RandRange(-5, 5), GD.RandRange(-5, 5));
await ToSignal(GetTree().CreateTimer(0.05f), "timeout");
camera.Offset = offset;

// Hitstop
Engine.TimeScale = 0.05f;
await ToSignal(GetTree().CreateTimer(0.05f), "timeout");
Engine.TimeScale = 1f;

// Scale pop
_sprite.Scale = Vector2.One * 1.2f;
var tween = CreateTween();
tween.TweenProperty(_sprite, "scale", Vector2.One, 0.1f);
```

---

## 📦 Instantiation

```csharp
// Load and instantiate scene
var scene = GD.Load<PackedScene>("res://scenes/enemy.tscn");
var instance = scene.Instantiate<Enemy>();
GetTree().CurrentScene.AddChild(instance);
instance.GlobalPosition = spawnPosition;

// With exported PackedScene
[Export] public PackedScene BulletScene;

var bullet = BulletScene.Instantiate<Bullet>();
AddChild(bullet);
bullet.GlobalPosition = _muzzle.GlobalPosition;
bullet.Direction = _aimDirection;
```

---

## 💾 Save/Load Basics

```csharp
// Save path
string path = "user://savegame.json";

// Save data
var data = new Godot.Collections.Dictionary
{
    { "health", _health },
    { "position_x", GlobalPosition.X },
    { "position_y", GlobalPosition.Y }
};
string json = Json.Stringify(data);
using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
file.StoreString(json);

// Load data
using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
string json = file.GetAsText();
var data = Json.ParseString(json).AsGodotDictionary();
_health = (float)data["health"];
GlobalPosition = new Vector2((float)data["position_x"], (float)data["position_y"]);

// Check if save exists
if (FileAccess.FileExists(path))
```

---

## 🔧 Utility Snippets

```csharp
// Null-safe call
_weapon?.Attack();

// Null coalescing
var name = _weapon?.Name ?? "Unarmed";

// Clamp value
health = Mathf.Clamp(health, 0, maxHealth);

// Remap value (e.g., 0-100 to 0-1)
float normalized = Mathf.Remap(value, 0, 100, 0, 1);

// Direction to angle
float angle = direction.Angle();

// Angle to direction
Vector2 dir = Vector2.Right.Rotated(angle);

// Degrees to radians
float rad = Mathf.DegToRad(45);

// Radians to degrees
float deg = Mathf.RadToDeg(Mathf.Pi);

// Check if point is in rect
if (rect.HasPoint(point))

// Get viewport size
Vector2 viewportSize = GetViewportRect().Size;

// Print debug
GD.Print($"Health: {health}, Position: {Position}");

// Print error
GD.PrintErr("Something went wrong!");
```

---

## 🎯 Common Math

```csharp
// Distance between two points
float dist = pos1.DistanceTo(pos2);

// Direction from A to B
Vector2 dir = (posB - posA).Normalized();

// Move toward target
pos = pos.MoveToward(target, speed * delta);

// Lerp (0-1 progress)
value = Mathf.Lerp(start, end, t);
position = startPos.Lerp(endPos, t);

// Smooth lerp (frame-rate independent)
position = position.Lerp(target, 1 - Mathf.Pow(0.1f, delta));

// Wrap value (e.g., angle)
angle = Mathf.Wrap(angle, 0, Mathf.Tau);

// Sign (-1, 0, or 1)
int sign = Mathf.Sign(value);

// Absolute value
float abs = Mathf.Abs(value);

// Round to nearest integer
int rounded = Mathf.RoundToInt(value);

// Floor (round down)
int floored = Mathf.FloorToInt(value);

// Ceil (round up)
int ceiled = Mathf.CeilToInt(value);
```

---

## 🎮 Common Patterns

```csharp
// Cooldown pattern
private float _cooldown;

public override void _Process(double delta)
{
    _cooldown = Mathf.Max(0, _cooldown - (float)delta);
}

public void TryAction()
{
    if (_cooldown > 0) return;
    
    DoAction();
    _cooldown = CooldownDuration;
}

// Singleton access
public static GameManager Instance { get; private set; }

public override void _Ready()
{
    Instance = this;
}

// Pool pattern (reuse objects)
private Queue<Bullet> _bulletPool = new();

public Bullet GetBullet()
{
    if (_bulletPool.Count > 0)
    {
        var bullet = _bulletPool.Dequeue();
        bullet.Reset();
        return bullet;
    }
    return BulletScene.Instantiate<Bullet>();
}

public void ReturnBullet(Bullet bullet)
{
    bullet.Visible = false;
    _bulletPool.Enqueue(bullet);
}
```

---

*Keep this reference handy while coding!* 📋

