# Chapter 05: Movement & Physics

> **Goal**: Build responsive, polished character movement.

---

## 🏃 CharacterBody2D Basics

`CharacterBody2D` is for entities you control directly (players, enemies):

```csharp
public partial class Player : CharacterBody2D
{
    [Export] public float MoveSpeed = 100f;
    
    public override void _PhysicsProcess(double delta)
    {
        // Get input
        Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        
        // Set velocity
        Velocity = input * MoveSpeed;
        
        // Move and handle collisions
        MoveAndSlide();
    }
}
```

### MoveAndSlide() Does:
1. Moves the body by `Velocity * delta`
2. Detects collisions
3. Slides along surfaces
4. Handles slopes (if enabled)

---

## 🎮 Movement Patterns

### Pattern 1: Instant Movement

Simple, direct control:

```csharp
Vector2 input = Input.GetVector("left", "right", "up", "down");
Velocity = input * MoveSpeed;
MoveAndSlide();
```

**Feel**: Arcade, responsive, immediate

### Pattern 2: Acceleration-Based

More realistic, momentum-based:

```csharp
[Export] public float MaxSpeed = 200f;
[Export] public float Acceleration = 800f;
[Export] public float Friction = 600f;

public override void _PhysicsProcess(double delta)
{
    float dt = (float)delta;
    Vector2 input = Input.GetVector("left", "right", "up", "down");
    
    if (input != Vector2.Zero)
    {
        // Accelerate toward input direction
        Velocity = Velocity.MoveToward(input * MaxSpeed, Acceleration * dt);
    }
    else
    {
        // Decelerate to stop
        Velocity = Velocity.MoveToward(Vector2.Zero, Friction * dt);
    }
    
    MoveAndSlide();
}
```

**Feel**: Weighty, realistic, ice-like at low friction

### Pattern 3: Velocity with Speed Cap

Direct control with speed limit:

```csharp
[Export] public float Acceleration = 1000f;
[Export] public float MaxSpeed = 150f;
[Export] public float Friction = 500f;

public override void _PhysicsProcess(double delta)
{
    float dt = (float)delta;
    Vector2 input = Input.GetVector("left", "right", "up", "down");
    
    if (input != Vector2.Zero)
    {
        // Add velocity in input direction
        Velocity += input * Acceleration * dt;
        
        // Cap speed
        if (Velocity.Length() > MaxSpeed)
        {
            Velocity = Velocity.Normalized() * MaxSpeed;
        }
    }
    else
    {
        // Apply friction
        Velocity = Velocity.MoveToward(Vector2.Zero, Friction * dt);
    }
    
    MoveAndSlide();
}
```

---

## 🏃‍♂️ Dash / Dodge

### Simple Dash

```csharp
[Export] public float DashSpeed = 400f;
[Export] public float DashDuration = 0.2f;

private bool _isDashing;
private Vector2 _dashDirection;
private float _dashTimer;

public void StartDash(Vector2 direction)
{
    if (_isDashing) return;
    if (direction == Vector2.Zero) return;
    
    _isDashing = true;
    _dashDirection = direction.Normalized();
    _dashTimer = DashDuration;
}

public override void _PhysicsProcess(double delta)
{
    if (_isDashing)
    {
        _dashTimer -= (float)delta;
        Velocity = _dashDirection * DashSpeed;
        
        if (_dashTimer <= 0)
        {
            _isDashing = false;
        }
        
        MoveAndSlide();
        return;  // Skip normal movement
    }
    
    // Normal movement...
}
```

### Dash with Curve

For more dynamic feel:

```csharp
public void StartDash(Vector2 direction)
{
    _isDashing = true;
    _dashDirection = direction.Normalized();
    _dashTimer = DashDuration;
    _dashStartSpeed = DashSpeed;
}

public override void _PhysicsProcess(double delta)
{
    if (_isDashing)
    {
        _dashTimer -= (float)delta;
        
        // Speed decreases over dash duration (ease out)
        float progress = 1 - (_dashTimer / DashDuration);
        float currentSpeed = Mathf.Lerp(_dashStartSpeed, DashSpeed * 0.3f, progress);
        
        Velocity = _dashDirection * currentSpeed;
        
        if (_dashTimer <= 0)
            _isDashing = false;
        
        MoveAndSlide();
        return;
    }
}
```

---

## 🛑 Knockback

### Separate Knockback Velocity

```csharp
private Vector2 _knockbackVelocity;
private const float KnockbackFriction = 800f;

public void ApplyKnockback(Vector2 direction, float force)
{
    _knockbackVelocity = direction.Normalized() * force;
}

public override void _PhysicsProcess(double delta)
{
    float dt = (float)delta;
    
    // Process knockback
    if (_knockbackVelocity.Length() > 10f)
    {
        _knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, KnockbackFriction * dt);
    }
    else
    {
        _knockbackVelocity = Vector2.Zero;
    }
    
    // Combine with movement
    Vector2 movementVelocity = GetMovementInput() * MoveSpeed;
    Velocity = movementVelocity + _knockbackVelocity;
    
    MoveAndSlide();
}
```

---

## 🔄 Rotation & Facing

### Rotate Toward Target

```csharp
// Instant rotation
Rotation = (target.GlobalPosition - GlobalPosition).Angle();

// Smooth rotation
float targetAngle = (target.GlobalPosition - GlobalPosition).Angle();
Rotation = Mathf.LerpAngle(Rotation, targetAngle, RotationSpeed * (float)delta);

// Rotation with max speed
float diff = Mathf.AngleDifference(Rotation, targetAngle);
Rotation += Mathf.Clamp(diff, -MaxRotation * (float)delta, MaxRotation * (float)delta);
```

### Face Movement Direction

```csharp
private Vector2 _facingDirection = Vector2.Right;

public override void _PhysicsProcess(double delta)
{
    Vector2 input = GetMovementInput();
    
    if (input != Vector2.Zero)
    {
        _facingDirection = input.Normalized();
        UpdateFacingSprite();
    }
    
    // ...
}

private void UpdateFacingSprite()
{
    if (Mathf.Abs(_facingDirection.X) > Mathf.Abs(_facingDirection.Y))
    {
        // Horizontal
        _sprite.FlipH = _facingDirection.X < 0;
        _sprite.Play(_facingDirection.X > 0 ? "walk_right" : "walk_right");
    }
    else
    {
        // Vertical
        _sprite.Play(_facingDirection.Y > 0 ? "walk_down" : "walk_up");
    }
}
```

---

## 💨 Velocity Modifiers

### Speed Buffs/Debuffs

```csharp
private float _speedMultiplier = 1f;
private List<SpeedModifier> _modifiers = new();

public void AddSpeedModifier(string id, float multiplier, float duration)
{
    _modifiers.Add(new SpeedModifier(id, multiplier, duration));
    RecalculateSpeedMultiplier();
}

private void RecalculateSpeedMultiplier()
{
    _speedMultiplier = 1f;
    foreach (var mod in _modifiers)
    {
        _speedMultiplier *= mod.Multiplier;
    }
}

public override void _PhysicsProcess(double delta)
{
    // Update modifiers
    _modifiers.RemoveAll(m => {
        m.TimeRemaining -= (float)delta;
        return m.TimeRemaining <= 0;
    });
    
    if (_modifiers.Any(m => m.TimeRemaining <= 0))
        RecalculateSpeedMultiplier();
    
    // Apply modified speed
    Vector2 input = GetMovementInput();
    Velocity = input * MoveSpeed * _speedMultiplier;
    MoveAndSlide();
}
```

---

## 🧱 Collision Detection

### Getting Collision Info

```csharp
public override void _PhysicsProcess(double delta)
{
    Velocity = ...;
    MoveAndSlide();
    
    // Check collisions after moving
    for (int i = 0; i < GetSlideCollisionCount(); i++)
    {
        var collision = GetSlideCollision(i);
        var collider = collision.GetCollider();
        
        GD.Print($"Hit: {collider.Name} at {collision.GetPosition()}");
        GD.Print($"Normal: {collision.GetNormal()}");
        
        if (collider is IDamageable damageable)
        {
            // We hit something we can damage
        }
    }
}
```

### Wall Detection

```csharp
public bool IsAgainstWall()
{
    for (int i = 0; i < GetSlideCollisionCount(); i++)
    {
        var collision = GetSlideCollision(i);
        var normal = collision.GetNormal();
        
        // Wall if normal is mostly horizontal
        if (Mathf.Abs(normal.X) > 0.7f)
            return true;
    }
    return false;
}
```

---

## 🎯 Movement Best Practices

### 1. Use _PhysicsProcess for Movement

```csharp
// CORRECT: Physics-based operations
public override void _PhysicsProcess(double delta)
{
    Velocity = ...;
    MoveAndSlide();
}

// WRONG: Can cause inconsistent movement
public override void _Process(double delta)
{
    MoveAndSlide();  // Don't do this
}
```

### 2. Always Normalize Input

```csharp
// WRONG: Diagonal is faster
Velocity = input * Speed;  // If input is (1,1), speed is 1.41x

// CORRECT: Consistent speed
Velocity = input.Normalized() * Speed;

// BEST: Input.GetVector already normalizes
Vector2 input = Input.GetVector("left", "right", "up", "down");
// Already normalized when magnitude > 1
```

### 3. Cast Delta to Float

```csharp
public override void _PhysicsProcess(double delta)
{
    // delta is double, but most operations want float
    float dt = (float)delta;
    
    Velocity += acceleration * dt;
}
```

### 4. Separate Concerns

```csharp
public override void _PhysicsProcess(double delta)
{
    // 1. Get input
    Vector2 input = GetMovementInput();
    
    // 2. Calculate desired velocity
    Vector2 desiredVelocity = CalculateVelocity(input, delta);
    
    // 3. Apply modifiers (knockback, buffs, etc.)
    desiredVelocity = ApplyModifiers(desiredVelocity);
    
    // 4. Set and move
    Velocity = desiredVelocity;
    MoveAndSlide();
    
    // 5. Handle post-movement (animations, effects)
    UpdateAnimations(input);
}
```

---

## 📋 Movement Checklist

- [ ] Using CharacterBody2D (not RigidBody2D for characters)
- [ ] Movement in _PhysicsProcess
- [ ] Input normalized for consistent diagonal speed
- [ ] Delta cast to float
- [ ] Acceleration/friction for polished feel
- [ ] Knockback system
- [ ] Speed modifiers support
- [ ] Facing direction tracked

---

**Next Chapter**: [06 - Animation System](06_ANIMATION_SYSTEM.md)

