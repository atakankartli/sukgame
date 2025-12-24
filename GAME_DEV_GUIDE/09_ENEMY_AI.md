# Chapter 09: Enemy AI

> **Goal**: Create engaging, believable enemy behaviors.

---

## 🧠 AI Design Principles

### The Three Components

```
DETECTION → DECISION → ACTION
    ↓           ↓          ↓
Can I see    What do    Execute
the player?  I do?      behavior
```

### Enemy Should Feel:
1. **Fair** - Telegraphed attacks, clear patterns
2. **Reactive** - Responds to player actions
3. **Interesting** - Not just "walk toward player"

---

## 👁️ Detection System

### Line of Sight

```csharp
public bool CanSeePlayer()
{
    var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
    if (player == null) return false;
    
    // Distance check first (cheap)
    float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
    if (distance > DetectionRange) return false;
    
    // Raycast for line of sight (expensive)
    var spaceState = GetWorld2D().DirectSpaceState;
    var query = PhysicsRayQueryParameters2D.Create(
        GlobalPosition,
        player.GlobalPosition,
        CollisionMask  // Set to walls layer
    );
    
    var result = spaceState.IntersectRay(query);
    
    // If nothing hit, we can see the player
    return result.Count == 0;
}
```

### Detection Area (Simpler)

```csharp
// Use an Area2D child node
public partial class Enemy : CharacterBody2D
{
    private Node2D _target;
    
    public override void _Ready()
    {
        var detectionArea = GetNode<Area2D>("DetectionArea");
        detectionArea.BodyEntered += OnBodyEntered;
        detectionArea.BodyExited += OnBodyExited;
    }
    
    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            _target = body;
        }
    }
    
    private void OnBodyExited(Node2D body)
    {
        if (body == _target)
        {
            _target = null;
        }
    }
}
```

### Field of View (Cone Detection)

```csharp
public bool IsInFieldOfView(Node2D target)
{
    var direction = (target.GlobalPosition - GlobalPosition).Normalized();
    var facing = Vector2.Right.Rotated(Rotation);  // Or your facing direction
    
    float angle = facing.AngleTo(direction);
    
    return Mathf.Abs(angle) < FieldOfViewAngle / 2;
}
```

---

## 🎯 Basic AI Patterns

### Pattern 1: Chase Player

```csharp
private void ChasePlayer(double delta)
{
    var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
    if (player == null) return;
    
    Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
    Velocity = direction * ChaseSpeed;
    MoveAndSlide();
}
```

### Pattern 2: Patrol Between Points

```csharp
[Export] public Vector2[] PatrolPoints;
private int _currentPoint;

private void Patrol(double delta)
{
    if (PatrolPoints.Length == 0) return;
    
    Vector2 target = PatrolPoints[_currentPoint];
    Vector2 direction = (target - GlobalPosition).Normalized();
    
    Velocity = direction * PatrolSpeed;
    MoveAndSlide();
    
    // Reached point?
    if (GlobalPosition.DistanceTo(target) < 5f)
    {
        _currentPoint = (_currentPoint + 1) % PatrolPoints.Length;
        // Optional: wait at each point
    }
}
```

### Pattern 3: Wander Randomly

```csharp
private Vector2 _wanderTarget;
private float _wanderTimer;

private void Wander(double delta)
{
    _wanderTimer -= (float)delta;
    
    if (_wanderTimer <= 0 || GlobalPosition.DistanceTo(_wanderTarget) < 10f)
    {
        // Pick new random target
        _wanderTarget = GlobalPosition + new Vector2(
            (float)GD.RandRange(-100, 100),
            (float)GD.RandRange(-100, 100)
        );
        _wanderTimer = (float)GD.RandRange(2.0, 5.0);
    }
    
    Vector2 direction = (_wanderTarget - GlobalPosition).Normalized();
    Velocity = direction * WanderSpeed;
    MoveAndSlide();
}
```

### Pattern 4: Keep Distance (Ranged Enemy)

```csharp
private void KeepDistance(double delta)
{
    var player = GetPlayerTarget();
    if (player == null) return;
    
    float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
    Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
    
    if (distance < MinDistance)
    {
        // Too close, back away
        Velocity = -direction * MoveSpeed;
    }
    else if (distance > MaxDistance)
    {
        // Too far, move closer
        Velocity = direction * MoveSpeed;
    }
    else
    {
        // Good distance, strafe or stop
        Velocity = Vector2.Zero;
    }
    
    MoveAndSlide();
}
```

### Pattern 5: Surround Player

```csharp
private void SurroundPlayer(double delta)
{
    var player = GetPlayerTarget();
    if (player == null) return;
    
    // Get all enemies
    var enemies = GetTree().GetNodesInGroup("enemies");
    int myIndex = enemies.IndexOf(this);
    
    // Calculate angle around player
    float angleStep = Mathf.Tau / enemies.Count;
    float myAngle = angleStep * myIndex;
    
    // Target position on circle around player
    Vector2 offset = Vector2.Right.Rotated(myAngle) * SurroundRadius;
    Vector2 targetPos = player.GlobalPosition + offset;
    
    // Move toward target position
    Vector2 direction = (targetPos - GlobalPosition).Normalized();
    Velocity = direction * MoveSpeed;
    MoveAndSlide();
}
```

---

## ⚔️ Combat AI

### Attack Pattern

```csharp
private float _attackCooldown;

public override void _PhysicsProcess(double delta)
{
    _attackCooldown -= (float)delta;
    
    var player = GetPlayerTarget();
    if (player == null) return;
    
    float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
    
    if (distance <= AttackRange && _attackCooldown <= 0)
    {
        StartAttack();
        _attackCooldown = AttackCooldownDuration;
    }
    else if (distance > AttackRange)
    {
        ChasePlayer(delta);
    }
}
```

### Telegraphed Attacks (Fair Combat)

```csharp
// In AttackState
public override void Enter()
{
    _timer = WindUpTime;
    _phase = Phase.WindUp;
    
    // Visual telegraph
    _enemy.Sprite.Play("attack_windup");
    _enemy.ShowWarningIndicator();  // "!" or red flash
}

public override void PhysicsUpdate(double delta)
{
    _timer -= (float)delta;
    
    switch (_phase)
    {
        case Phase.WindUp:
            if (_timer <= 0)
            {
                _phase = Phase.Attack;
                _timer = AttackDuration;
                _enemy.PerformAttack();  // Actually deal damage
                _enemy.HideWarningIndicator();
            }
            break;
            
        case Phase.Attack:
            if (_timer <= 0)
            {
                _phase = Phase.Recovery;
                _timer = RecoveryTime;  // Vulnerable!
            }
            break;
            
        case Phase.Recovery:
            if (_timer <= 0)
            {
                Machine.ChangeState("Idle");
            }
            break;
    }
}
```

---

## 🗺️ Pathfinding

### Using NavigationAgent2D

```csharp
private NavigationAgent2D _navAgent;

public override void _Ready()
{
    _navAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");
    _navAgent.PathDesiredDistance = 4f;
    _navAgent.TargetDesiredDistance = 4f;
}

public void SetTarget(Vector2 target)
{
    _navAgent.TargetPosition = target;
}

public override void _PhysicsProcess(double delta)
{
    if (_navAgent.IsNavigationFinished()) return;
    
    Vector2 nextPos = _navAgent.GetNextPathPosition();
    Vector2 direction = (nextPos - GlobalPosition).Normalized();
    
    Velocity = direction * MoveSpeed;
    MoveAndSlide();
}
```

### Setup
1. Add `NavigationRegion2D` to your level
2. Assign a `NavigationPolygon` (draw walkable area)
3. Add `NavigationAgent2D` to enemy

---

## 🎭 Enemy State Machine

```csharp
public enum EnemyState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Stagger,
    Dead
}

private EnemyState _state = EnemyState.Patrol;

public override void _PhysicsProcess(double delta)
{
    switch (_state)
    {
        case EnemyState.Idle:
            HandleIdle(delta);
            break;
        case EnemyState.Patrol:
            HandlePatrol(delta);
            CheckForPlayer();
            break;
        case EnemyState.Chase:
            HandleChase(delta);
            CheckAttackRange();
            CheckLostPlayer();
            break;
        case EnemyState.Attack:
            HandleAttack(delta);
            break;
        case EnemyState.Stagger:
            HandleStagger(delta);
            break;
    }
}

private void CheckForPlayer()
{
    if (CanSeePlayer())
    {
        _state = EnemyState.Chase;
        // Alert sound, animation change
    }
}

private void CheckLostPlayer()
{
    if (!CanSeePlayer())
    {
        _lostPlayerTimer -= Time.DeltaTime;
        if (_lostPlayerTimer <= 0)
        {
            _state = EnemyState.Patrol;
        }
    }
    else
    {
        _lostPlayerTimer = LostPlayerDuration;  // Reset timer
    }
}
```

---

## 📊 AI Parameters as Resources

```csharp
[GlobalClass]
public partial class EnemyBehavior : Resource
{
    [ExportGroup("Detection")]
    [Export] public float DetectionRange = 100f;
    [Export] public float FieldOfView = 90f;  // Degrees
    [Export] public float LoseTargetTime = 3f;
    
    [ExportGroup("Movement")]
    [Export] public float PatrolSpeed = 30f;
    [Export] public float ChaseSpeed = 50f;
    
    [ExportGroup("Combat")]
    [Export] public float AttackRange = 20f;
    [Export] public float AttackCooldown = 2f;
    [Export] public float WindUpTime = 0.5f;
    [Export] public float RecoveryTime = 0.8f;
}
```

Now one Enemy script works for many enemy types by swapping the behavior resource!

---

## 📋 Enemy AI Checklist

- [ ] Detection system (area or raycast)
- [ ] State machine for behaviors
- [ ] Chase with pathfinding or direct movement
- [ ] Attack with wind-up (telegraph)
- [ ] Recovery period after attack (punishable)
- [ ] Stagger state when hit
- [ ] Lose target after time
- [ ] Audio/visual feedback on state changes

---

**Next Chapter**: [10 - UI & HUD](10_UI_HUD.md)

