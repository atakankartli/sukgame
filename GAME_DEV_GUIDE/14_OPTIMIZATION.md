# Chapter 14: Optimization

> **Goal**: Keep your game running smoothly at 60+ FPS.

---

## 📊 Profiling First!

**Rule #1**: Never optimize blindly. Profile first.

### Built-in Profiler

1. Run your game
2. Debugger panel → Profiler tab
3. Look for:
   - Functions taking >1ms per frame
   - Spikes in frame time
   - Physics vs Process time

### Debug Monitor

```csharp
// Show FPS in debug build
public override void _Process(double delta)
{
    if (OS.IsDebugBuild())
    {
        _fpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";
    }
}
```

---

## 🚀 Common Optimizations

### 1. Object Pooling

Don't create/destroy objects repeatedly:

```csharp
public partial class ProjectilePool : Node
{
    [Export] public PackedScene ProjectileScene;
    [Export] public int PoolSize = 50;
    
    private Queue<Projectile> _pool = new();
    
    public override void _Ready()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            var proj = ProjectileScene.Instantiate<Projectile>();
            proj.SetProcess(false);
            proj.Visible = false;
            AddChild(proj);
            _pool.Enqueue(proj);
        }
    }
    
    public Projectile Get()
    {
        if (_pool.Count == 0)
        {
            // Pool exhausted, create new (or return null)
            return ProjectileScene.Instantiate<Projectile>();
        }
        
        var proj = _pool.Dequeue();
        proj.SetProcess(true);
        proj.Visible = true;
        return proj;
    }
    
    public void Return(Projectile proj)
    {
        proj.SetProcess(false);
        proj.Visible = false;
        _pool.Enqueue(proj);
    }
}
```

### 2. Avoid GetNode Every Frame

```csharp
// ❌ BAD: GetNode every frame
public override void _Process(double delta)
{
    GetNode<Label>("HUD/Health").Text = health.ToString();
}

// ✅ GOOD: Cache in _Ready
private Label _healthLabel;

public override void _Ready()
{
    _healthLabel = GetNode<Label>("HUD/Health");
}

public override void _Process(double delta)
{
    _healthLabel.Text = health.ToString();
}
```

### 3. Don't Update UI Every Frame

```csharp
// ❌ BAD: Update every frame
public override void _Process(double delta)
{
    _healthBar.Value = _health;  // Even if health didn't change
}

// ✅ GOOD: Update only when changed
public void TakeDamage(float damage)
{
    _health -= damage;
    _healthBar.Value = _health;  // Only when health changes
}
```

### 4. Reduce Physics Checks

```csharp
// Disable processing when not needed
public void OnBecomeInactive()
{
    SetPhysicsProcess(false);
    SetProcess(false);
}

public void OnBecomeActive()
{
    SetPhysicsProcess(true);
    SetProcess(true);
}
```

### 5. Use Visibility Notifiers

```csharp
public partial class Enemy : CharacterBody2D
{
    private VisibleOnScreenNotifier2D _visibility;
    
    public override void _Ready()
    {
        _visibility = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
        _visibility.ScreenEntered += () => SetProcess(true);
        _visibility.ScreenExited += () => SetProcess(false);
    }
}
```

---

## 🎨 Rendering Optimization

### 1. Limit Draw Calls

- Combine sprites into texture atlases
- Use `CanvasGroup` for grouped effects
- Avoid overlapping semi-transparent sprites

### 2. Particle Optimization

```
GPUParticles2D Settings:
├── Amount: Keep low (50-200)
├── Lifetime: Keep short
├── Fixed FPS: 30 (for non-critical particles)
└── Fract Delta: Enable for smoother
```

### 3. Shader Optimization

```glsl
// ❌ Expensive: Complex per-pixel math
COLOR = texture(TEXTURE, UV + sin(TIME * 10.0) * 0.1);

// ✅ Cheaper: Simpler operations
COLOR = texture(TEXTURE, UV) * MODULATE;
```

---

## 🔢 Physics Optimization

### 1. Collision Layers

Use layers to reduce collision checks:

```
Layer 1: Player
Layer 2: Player Projectiles
Layer 3: Enemies
Layer 4: Enemy Projectiles
Layer 5: Walls

Player Mask: 3, 4, 5 (checks against enemies, enemy bullets, walls)
Enemy Mask: 1, 2, 5 (checks against player, player bullets, walls)
```

### 2. Simple Collision Shapes

```
Speed (fast to slow):
CircleShape2D → CapsuleShape2D → RectangleShape2D → ConvexPolygonShape2D → ConcavePolygonShape2D

Use the simplest shape that works!
```

### 3. Reduce Physics Bodies

```csharp
// For decorations/static objects, don't use physics bodies
// Use StaticBody2D only if collision is needed
// Use Sprite2D alone for pure visuals
```

---

## 🧠 Memory Management

### 1. Unload Unused Resources

```csharp
// Resources are cached, manually clear if needed
ResourceLoader.Load("res://textures/large_texture.png").Unreference();
```

### 2. Free Nodes Properly

```csharp
// Always use QueueFree for nodes
enemy.QueueFree();  // Safe, waits until end of frame

// Don't use Free() unless you're sure no signals are pending
```

### 3. Weak References for Targets

```csharp
// Use WeakRef to avoid holding references to freed objects
private WeakRef _targetRef;

public void SetTarget(Node2D target)
{
    _targetRef = GodotObject.WeakRef(target);
}

public override void _Process(double delta)
{
    var target = _targetRef?.GetRef() as Node2D;
    if (target == null || !IsInstanceValid(target))
    {
        // Target gone, find new one
        return;
    }
    
    // Use target
}
```

---

## ⏱️ Delta Time Consistency

### Fixed Physics Rate

In Project Settings → Physics → Common:
- Physics FPS: 60 (or 30 for slower games)

```csharp
// Physics runs at fixed rate
public override void _PhysicsProcess(double delta)
{
    // delta is always ~0.016667 at 60 FPS
}

// Process can vary
public override void _Process(double delta)
{
    // delta varies based on actual framerate
    // Always multiply by delta for time-based things
}
```

---

## 📈 Performance Patterns

### LOD (Level of Detail)

```csharp
public override void _Process(double delta)
{
    var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
    float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
    
    if (distance > 1000)
    {
        // Far away: simplified behavior
        SetPhysicsProcess(false);
        _sprite.Visible = false;
    }
    else if (distance > 500)
    {
        // Medium: reduced updates
        _updateRate = 0.1f;  // Update 10x per second
    }
    else
    {
        // Close: full quality
        SetPhysicsProcess(true);
        _sprite.Visible = true;
        _updateRate = 0f;  // Every frame
    }
}
```

### Spatial Hashing for Many Entities

```csharp
// Instead of checking all entities against all entities
// Group them by grid cell

private Dictionary<Vector2I, List<Entity>> _grid = new();

public void UpdateGrid(Entity entity)
{
    var cell = (Vector2I)(entity.GlobalPosition / CellSize);
    
    // Remove from old cell, add to new
    if (_entityCells.TryGetValue(entity, out var oldCell))
        _grid[oldCell].Remove(entity);
    
    if (!_grid.ContainsKey(cell))
        _grid[cell] = new List<Entity>();
    
    _grid[cell].Add(entity);
    _entityCells[entity] = cell;
}

public List<Entity> GetNearby(Vector2 position)
{
    var cell = (Vector2I)(position / CellSize);
    var nearby = new List<Entity>();
    
    // Check 3x3 grid around position
    for (int x = -1; x <= 1; x++)
    for (int y = -1; y <= 1; y++)
    {
        var checkCell = cell + new Vector2I(x, y);
        if (_grid.TryGetValue(checkCell, out var entities))
            nearby.AddRange(entities);
    }
    
    return nearby;
}
```

---

## 📋 Optimization Checklist

### Before Shipping:
- [ ] Profile the game, find actual bottlenecks
- [ ] Object pool frequently created/destroyed objects
- [ ] Cache GetNode results in _Ready
- [ ] Use visibility notifiers to disable off-screen entities
- [ ] Simplify collision shapes
- [ ] Set up proper collision layers
- [ ] Disable processing on inactive objects
- [ ] Test on target minimum hardware

### Common Culprits:
- [ ] Too many physics bodies
- [ ] GetNode/Find operations every frame
- [ ] UI updates every frame (vs. on change)
- [ ] Complex shaders on many objects
- [ ] Unoptimized particles
- [ ] Memory leaks from not freeing nodes

---

**Next Chapter**: [15 - Isometric Tips](15_ISOMETRIC_TIPS.md)

