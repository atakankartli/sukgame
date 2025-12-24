# Chapter 16: Common Patterns & Architecture

> **Goal**: Learn reusable patterns that solve common game dev problems.

---

## 🔄 The Singleton Pattern

For managers that need global access (use sparingly!):

### AutoLoad Singleton

```csharp
// scripts/Core/GameManager.cs
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }
    
    public bool IsPaused { get; private set; }
    public int Score { get; private set; }
    
    public override void _Ready()
    {
        Instance = this;
    }
    
    public void AddScore(int amount)
    {
        Score += amount;
    }
    
    public void Pause()
    {
        IsPaused = true;
        GetTree().Paused = true;
    }
}
```

**Setup**: Project → Project Settings → AutoLoad → Add GameManager.cs

**Usage**:
```csharp
GameManager.Instance.AddScore(100);
```

### When to Use Singletons

✅ **Good uses**:
- GameManager (pause, score, game state)
- AudioManager (play sounds from anywhere)
- EventBus (global event system)
- SaveManager (save/load data)

❌ **Avoid for**:
- Player reference (use groups instead)
- Level-specific data
- Anything that should have multiple instances

---

## 📡 The Event Bus Pattern

Decoupled communication without direct references:

```csharp
// scripts/Core/EventBus.cs
public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }
    
    // Game events
    public event Action<int> ScoreChanged;
    public event Action PlayerDied;
    public event Action<string> LevelCompleted;
    public event Action<Enemy> EnemyKilled;
    public event Action GamePaused;
    public event Action GameResumed;
    
    public override void _Ready()
    {
        Instance = this;
    }
    
    // Emit methods (provide null safety)
    public void OnScoreChanged(int newScore) => ScoreChanged?.Invoke(newScore);
    public void OnPlayerDied() => PlayerDied?.Invoke();
    public void OnLevelCompleted(string levelName) => LevelCompleted?.Invoke(levelName);
    public void OnEnemyKilled(Enemy enemy) => EnemyKilled?.Invoke(enemy);
}
```

**Publisher (emits events)**:
```csharp
// In Enemy.cs
private void Die()
{
    EventBus.Instance.OnEnemyKilled(this);
    QueueFree();
}
```

**Subscriber (listens for events)**:
```csharp
// In HUD.cs
public override void _Ready()
{
    EventBus.Instance.EnemyKilled += OnEnemyKilled;
    EventBus.Instance.ScoreChanged += OnScoreChanged;
}

public override void _ExitTree()
{
    // Always unsubscribe!
    EventBus.Instance.EnemyKilled -= OnEnemyKilled;
    EventBus.Instance.ScoreChanged -= OnScoreChanged;
}

private void OnEnemyKilled(Enemy enemy)
{
    _killCount++;
    UpdateKillDisplay();
}
```

---

## 🏭 The Object Pool Pattern

Reuse objects instead of creating/destroying:

```csharp
// scripts/Core/ObjectPool.cs
public partial class ObjectPool<T> : Node where T : Node, new()
{
    [Export] public PackedScene Prefab;
    [Export] public int InitialSize = 10;
    
    private Queue<T> _available = new();
    private List<T> _active = new();
    
    public override void _Ready()
    {
        // Pre-populate pool
        for (int i = 0; i < InitialSize; i++)
        {
            var instance = CreateInstance();
            instance.Visible = false;
            _available.Enqueue(instance);
        }
    }
    
    public T Get()
    {
        T instance;
        
        if (_available.Count > 0)
        {
            instance = _available.Dequeue();
        }
        else
        {
            instance = CreateInstance();
        }
        
        instance.Visible = true;
        _active.Add(instance);
        return instance;
    }
    
    public void Return(T instance)
    {
        instance.Visible = false;
        _active.Remove(instance);
        _available.Enqueue(instance);
    }
    
    private T CreateInstance()
    {
        var instance = Prefab.Instantiate<T>();
        AddChild(instance);
        return instance;
    }
}
```

**Usage (Bullets)**:
```csharp
public partial class BulletPool : ObjectPool<Bullet> { }

// In Player
private BulletPool _bulletPool;

public void Shoot()
{
    var bullet = _bulletPool.Get();
    bullet.GlobalPosition = _muzzle.GlobalPosition;
    bullet.Initialize(_aimDirection);
}

// In Bullet
public void OnHit()
{
    _pool.Return(this);  // Return instead of QueueFree
}
```

---

## 📦 The Component Pattern

Small, focused behaviors that combine:

```csharp
// Instead of one massive Player class...
Player (CharacterBody2D)
├── HealthComponent       ← Handles health, death
├── MovementComponent     ← Handles input, velocity
├── CombatComponent       ← Handles attacking
├── AnimationComponent    ← Handles sprite animations
└── AudioComponent        ← Handles sounds
```

```csharp
// scripts/Components/HealthComponent.cs
public partial class HealthComponent : Node
{
    [Signal] public delegate void DiedEventHandler();
    [Signal] public delegate void HealthChangedEventHandler(float current, float max);
    
    [Export] public float MaxHealth = 100f;
    public float CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    
    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }
    
    public void TakeDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        
        if (!IsAlive)
            EmitSignal(SignalName.Died);
    }
    
    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }
}
```

**In Parent Script**:
```csharp
public partial class Player : CharacterBody2D
{
    private HealthComponent _health;
    private MovementComponent _movement;
    
    public override void _Ready()
    {
        _health = GetNode<HealthComponent>("HealthComponent");
        _movement = GetNode<MovementComponent>("MovementComponent");
        
        _health.Died += OnDied;
    }
}
```

---

## 🗃️ The Resource Pattern

Use Resources for data that can be shared:

```csharp
// scripts/Resources/EnemyData.cs
[GlobalClass]
public partial class EnemyData : Resource
{
    [Export] public string EnemyName;
    [Export] public float MaxHealth = 50f;
    [Export] public float MoveSpeed = 40f;
    [Export] public float AttackDamage = 10f;
    [Export] public float AttackRange = 20f;
    [Export] public float DetectionRange = 100f;
    [Export] public PackedScene DeathEffect;
    [Export] public AudioStream[] HitSounds;
}
```

**Create Resources**:
1. Right-click in FileSystem
2. Create New → Resource
3. Select EnemyData
4. Fill in values
5. Save as `Goblin.tres`, `Skeleton.tres`, etc.

**Use in Enemy**:
```csharp
public partial class Enemy : CharacterBody2D
{
    [Export] public EnemyData Data;  // Drag .tres file here
    
    public override void _Ready()
    {
        _health.MaxHealth = Data.MaxHealth;
        _moveSpeed = Data.MoveSpeed;
    }
}
```

**Benefits**:
- One Enemy script, many enemy types
- Designers can edit .tres files without code
- Easy to compare/balance stats

---

## 🎭 The Strategy Pattern

Swap behaviors at runtime:

```csharp
// scripts/AI/AIBehavior.cs
public abstract partial class AIBehavior : Resource
{
    public abstract void Execute(Enemy enemy, double delta);
}

// scripts/AI/PatrolBehavior.cs
[GlobalClass]
public partial class PatrolBehavior : AIBehavior
{
    [Export] public float PatrolSpeed = 30f;
    [Export] public Vector2[] PatrolPoints;
    
    private int _currentPoint;
    
    public override void Execute(Enemy enemy, double delta)
    {
        if (PatrolPoints.Length == 0) return;
        
        var target = PatrolPoints[_currentPoint];
        var direction = (target - enemy.GlobalPosition).Normalized();
        
        enemy.Velocity = direction * PatrolSpeed;
        enemy.MoveAndSlide();
        
        if (enemy.GlobalPosition.DistanceTo(target) < 5f)
        {
            _currentPoint = (_currentPoint + 1) % PatrolPoints.Length;
        }
    }
}

// scripts/AI/ChaseBehavior.cs
[GlobalClass]
public partial class ChaseBehavior : AIBehavior
{
    [Export] public float ChaseSpeed = 60f;
    
    public override void Execute(Enemy enemy, double delta)
    {
        var player = enemy.GetPlayerTarget();
        if (player == null) return;
        
        var direction = (player.GlobalPosition - enemy.GlobalPosition).Normalized();
        enemy.Velocity = direction * ChaseSpeed;
        enemy.MoveAndSlide();
    }
}
```

**In Enemy**:
```csharp
[Export] public AIBehavior IdleBehavior;
[Export] public AIBehavior ChaseBehavior;

private AIBehavior _currentBehavior;

public override void _PhysicsProcess(double delta)
{
    // Switch behaviors based on conditions
    if (CanSeePlayer())
        _currentBehavior = ChaseBehavior;
    else
        _currentBehavior = IdleBehavior;
    
    _currentBehavior?.Execute(this, delta);
}
```

---

## 🎲 The Weighted Random Pattern

For loot drops, spawn chances, etc.:

```csharp
public class WeightedRandom<T>
{
    private List<(T item, float weight)> _items = new();
    private float _totalWeight;
    
    public void Add(T item, float weight)
    {
        _items.Add((item, weight));
        _totalWeight += weight;
    }
    
    public T GetRandom()
    {
        float roll = GD.Randf() * _totalWeight;
        float cumulative = 0f;
        
        foreach (var (item, weight) in _items)
        {
            cumulative += weight;
            if (roll < cumulative)
                return item;
        }
        
        return _items[^1].item;  // Fallback to last
    }
}

// Usage
var lootTable = new WeightedRandom<string>();
lootTable.Add("Gold", 50f);      // 50% chance
lootTable.Add("Potion", 30f);    // 30% chance
lootTable.Add("Rare Sword", 15f); // 15% chance
lootTable.Add("Epic Armor", 5f);  // 5% chance

string drop = lootTable.GetRandom();
```

---

## 🔔 The Observer Pattern (Signals)

Already covered, but here's a clean example:

```csharp
// Subject (emits events)
public partial class Chest : Area2D
{
    [Signal] public delegate void OpenedEventHandler(Chest chest);
    
    public void Open()
    {
        EmitSignal(SignalName.Opened, this);
        _sprite.Play("open");
    }
}

// Observer (reacts to events)
public partial class QuestManager : Node
{
    private int _chestsOpened;
    
    public void TrackChest(Chest chest)
    {
        chest.Opened += OnChestOpened;
    }
    
    private void OnChestOpened(Chest chest)
    {
        _chestsOpened++;
        CheckQuestProgress();
    }
}
```

---

## 📋 Pattern Selection Guide

| Problem | Pattern |
|---------|---------|
| Global access to manager | Singleton (AutoLoad) |
| Decoupled communication | Event Bus |
| Many similar objects | Object Pool |
| Reusable behaviors | Component |
| Configurable data | Resource |
| Swappable algorithms | Strategy |
| Random with weights | Weighted Random |
| React to changes | Observer (Signals) |

---

## ⚠️ Anti-Patterns to Avoid

### God Objects
```csharp
// BAD: One class does everything
public class GameManager
{
    public void UpdatePlayer() { }
    public void UpdateEnemies() { }
    public void UpdateUI() { }
    public void SaveGame() { }
    public void LoadGame() { }
    public void PlaySound() { }
    // ... 1000 more lines
}
```

### Deep Nesting
```csharp
// BAD
GetTree().CurrentScene.GetNode("Level").GetNode("Enemies").GetNode("Goblin").GetNode("Health")

// GOOD: Use groups or direct references
var enemies = GetTree().GetNodesInGroup("enemies");
```

### Hardcoded Values
```csharp
// BAD
if (health < 50)  // Magic number

// GOOD
private const float LowHealthThreshold = 50f;
if (health < LowHealthThreshold)

// BEST: Export it
[Export] public float LowHealthThreshold = 50f;
```

---

**Next**: [Appendix A - Cheat Sheets](A_CHEAT_SHEETS.md)

