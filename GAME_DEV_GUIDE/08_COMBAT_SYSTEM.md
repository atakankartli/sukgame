# Chapter 08: Combat System

> **Goal**: Build a flexible, data-driven combat system with satisfying feedback.

---

## 🏗️ Combat System Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         COMBAT PIPELINE                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Attack ──► Hitbox ──► Hurtbox ──► CombatStats ──► Damage Result   │
│    │          │           │            │               │            │
│    │          │           │            │               ▼            │
│  Weapon    Collision   Validation   Processing    ┌─────────┐      │
│  Data      Detection   (is alive?)  (defense,     │ Effects │      │
│                                      crits, etc)  ├─────────┤      │
│                                                   │Knockback│      │
│                                                   │Flash    │      │
│                                                   │Sound    │      │
│                                                   │Shake    │      │
│                                                   └─────────┘      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 📦 Core Components

### 1. DamageInfo (Data Transfer Object)

Carries all information about a damage instance:

```csharp
public struct DamageInfo
{
    public float BaseDamage;       // Raw damage
    public Node2D Source;          // Who attacked
    public Vector2 Direction;      // For knockback
    public DamageType Type;        // Physical, Fire, etc.
    public float KnockbackForce;   // Push strength
    public float PoiseDamage;      // Stagger buildup
    public bool CanCrit;           // Can this crit?
    public bool WasCrit;           // Was it a crit? (set during processing)
    public float FinalDamage;      // After all modifiers
    
    // Factory methods for common cases
    public static DamageInfo Physical(float damage, Node2D source, Vector2 dir)
    {
        return new DamageInfo
        {
            BaseDamage = damage,
            Source = source,
            Direction = dir.Normalized(),
            Type = DamageType.Physical,
            KnockbackForce = 100f,
            PoiseDamage = damage * 0.5f,
            CanCrit = true
        };
    }
}
```

### 2. IDamageable (Interface)

Any entity that can take damage implements this:

```csharp
public interface IDamageable
{
    float TakeDamage(DamageInfo info);
    bool IsAlive { get; }
    float CurrentHealth { get; }
    float MaxHealth { get; }
}
```

### 3. CombatStats (Component)

Handles health, defense, and damage processing:

```csharp
public partial class CombatStats : Node
{
    // Signals for UI and effects
    [Signal] public delegate void HealthChangedEventHandler(float current, float max);
    [Signal] public delegate void DiedEventHandler();
    [Signal] public delegate void PoiseBrokenEventHandler();
    
    // C# event for damage (can carry DamageInfo)
    public event Action<DamageInfo> DamageTaken;
    
    [Export] public float MaxHealth = 100f;
    [Export] public float Defense = 0f;
    [Export] public float MaxPoise = 50f;
    
    public float CurrentHealth { get; private set; }
    public float CurrentPoise { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public bool IsInvincible { get; private set; }
    
    public float ProcessDamage(ref DamageInfo info)
    {
        if (!IsAlive || IsInvincible) return 0f;
        
        float damage = info.BaseDamage;
        
        // Apply defense
        damage = Mathf.Max(0, damage - Defense);
        
        // Apply poise damage
        CurrentPoise -= info.PoiseDamage;
        if (CurrentPoise <= 0)
        {
            EmitSignal(SignalName.PoiseBroken);
            CurrentPoise = MaxPoise;  // Reset
        }
        
        // Apply health damage
        info.FinalDamage = damage;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        
        // Fire events
        DamageTaken?.Invoke(info);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        
        if (!IsAlive)
            EmitSignal(SignalName.Died);
        
        return damage;
    }
}
```

### 4. Hitbox (Deals Damage)

Attach to weapons, projectiles, hazards:

```csharp
public partial class Hitbox : Area2D
{
    [Export] public float BaseDamage = 10f;
    [Export] public float KnockbackForce = 100f;
    [Export] public bool Active = false;
    
    public Node2D HitboxOwner;  // Don't hit yourself
    private HashSet<Hurtbox> _hitTargets = new();
    
    public override void _Ready()
    {
        AreaEntered += OnAreaEntered;
    }
    
    private void OnAreaEntered(Area2D area)
    {
        if (!Active) return;
        if (area is not Hurtbox hurtbox) return;
        if (!hurtbox.Active) return;
        
        // Don't hit owner
        if (HitboxOwner != null && hurtbox.GetOwner() == HitboxOwner) return;
        
        // Don't hit same target twice
        if (_hitTargets.Contains(hurtbox)) return;
        _hitTargets.Add(hurtbox);
        
        // Build damage info
        var direction = (hurtbox.GlobalPosition - GlobalPosition).Normalized();
        var info = DamageInfo.Physical(BaseDamage, HitboxOwner, direction);
        info.KnockbackForce = KnockbackForce;
        
        // Deal damage
        hurtbox.ReceiveDamage(info);
    }
    
    public void Activate()
    {
        Active = true;
        _hitTargets.Clear();
    }
    
    public void Deactivate()
    {
        Active = false;
    }
}
```

### 5. Hurtbox (Receives Damage)

Attach to entities that can be hurt:

```csharp
public partial class Hurtbox : Area2D
{
    public event Action<DamageInfo> Hurt;
    
    [Export] public bool Active = true;
    
    private CombatStats _stats;
    
    public override void _Ready()
    {
        _stats = GetParent().GetNodeOrNull<CombatStats>("CombatStats");
    }
    
    public float ReceiveDamage(DamageInfo info)
    {
        if (!Active || _stats == null || !_stats.IsAlive) return 0f;
        
        float damage = _stats.ProcessDamage(ref info);
        
        if (damage > 0)
            Hurt?.Invoke(info);
        
        return damage;
    }
    
    public Node2D GetOwner()
    {
        return GetParent() as Node2D;
    }
}
```

---

## ⚔️ Weapon System

### Weapon Resource (Data)

```csharp
[GlobalClass]
public partial class Weapon : Resource
{
    [ExportGroup("Info")]
    [Export] public string WeaponName = "Weapon";
    [Export] public Texture2D Icon;
    
    [ExportGroup("Damage")]
    [Export] public float BaseDamage = 10f;
    [Export] public DamageType DamageType = DamageType.Physical;
    [Export] public float KnockbackForce = 80f;
    [Export] public float PoiseDamage = 15f;
    
    [ExportGroup("Timing")]
    [Export] public float WindUpTime = 0.1f;    // Before hit
    [Export] public float ActiveTime = 0.15f;   // Hitbox active
    [Export] public float RecoveryTime = 0.2f;  // After hit
    
    public float TotalAttackTime => WindUpTime + ActiveTime + RecoveryTime;
    
    [ExportGroup("Hitbox")]
    [Export] public Vector2 HitboxSize = new Vector2(24, 16);
    [Export] public float HitboxDistance = 16f;
}
```

### MeleeWeapon Component

Manages attacks and hitbox timing:

```csharp
public partial class MeleeWeapon : Node2D
{
    [Export] public Weapon EquippedWeapon;
    
    public bool IsAttacking { get; private set; }
    
    private Hitbox _hitbox;
    private float _attackTimer;
    private AttackPhase _phase;
    
    private enum AttackPhase { None, WindUp, Active, Recovery }
    
    public bool Attack(Vector2 direction)
    {
        if (IsAttacking || EquippedWeapon == null) return false;
        
        IsAttacking = true;
        _phase = AttackPhase.WindUp;
        _attackTimer = EquippedWeapon.WindUpTime;
        
        PositionHitbox(direction);
        UpdateHitboxStats();
        
        return true;
    }
    
    public override void _Process(double delta)
    {
        if (!IsAttacking) return;
        
        _attackTimer -= (float)delta;
        
        if (_attackTimer <= 0)
        {
            switch (_phase)
            {
                case AttackPhase.WindUp:
                    _phase = AttackPhase.Active;
                    _attackTimer = EquippedWeapon.ActiveTime;
                    _hitbox.Activate();
                    break;
                    
                case AttackPhase.Active:
                    _phase = AttackPhase.Recovery;
                    _attackTimer = EquippedWeapon.RecoveryTime;
                    _hitbox.Deactivate();
                    break;
                    
                case AttackPhase.Recovery:
                    EndAttack();
                    break;
            }
        }
    }
}
```

---

## 💥 Hit Feedback (Game Feel)

Great combat FEELS impactful. Here's how:

### 1. Screen Shake

```csharp
public void DoScreenShake(float intensity, float duration)
{
    var camera = GetViewport().GetCamera2D();
    if (camera == null) return;
    
    var originalOffset = camera.Offset;
    var tween = CreateTween();
    
    // Shake by setting random offsets
    for (int i = 0; i < 5; i++)
    {
        tween.TweenProperty(camera, "offset", 
            originalOffset + new Vector2(
                GD.RandRange(-intensity, intensity),
                GD.RandRange(-intensity, intensity)
            ), duration / 10);
    }
    
    // Return to original
    tween.TweenProperty(camera, "offset", originalOffset, duration / 10);
}
```

### 2. Hitstop (Freeze Frame)

```csharp
public void DoHitstop(float duration = 0.05f)
{
    Engine.TimeScale = 0.05f;  // Nearly frozen
    
    GetTree().CreateTimer(duration).Timeout += () => {
        Engine.TimeScale = 1f;
    };
}
```

### 3. Flash White

```csharp
public void DoFlash(Sprite2D sprite, float duration = 0.1f)
{
    sprite.Modulate = Colors.White;
    
    var tween = CreateTween();
    tween.TweenProperty(sprite, "modulate", Colors.White * 1.5f, duration / 2);
    tween.TweenProperty(sprite, "modulate", Colors.White, duration / 2);
}
```

### 4. Damage Numbers

```csharp
public void SpawnDamageNumber(Vector2 position, float damage, bool isCrit)
{
    var label = new Label();
    label.Text = Mathf.RoundToInt(damage).ToString();
    label.GlobalPosition = position + new Vector2(0, -20);
    label.AddThemeColorOverride("font_color", isCrit ? Colors.Yellow : Colors.White);
    label.AddThemeFontSizeOverride("font_size", isCrit ? 16 : 12);
    
    GetTree().CurrentScene.AddChild(label);
    
    var tween = CreateTween();
    tween.TweenProperty(label, "position:y", label.Position.Y - 30, 0.5f);
    tween.Parallel().TweenProperty(label, "modulate:a", 0f, 0.5f);
    tween.TweenCallback(Callable.From(() => label.QueueFree()));
}
```

### 5. Knockback

```csharp
public partial class KnockbackReceiver : Node
{
    [Export] public float KnockbackMultiplier = 1f;
    [Export] public float Friction = 800f;
    
    private CharacterBody2D _body;
    private Vector2 _knockbackVelocity;
    public bool IsKnockedBack => _knockbackVelocity.Length() > 10f;
    
    public void ApplyKnockback(Vector2 direction, float force)
    {
        _knockbackVelocity = direction.Normalized() * force * KnockbackMultiplier;
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (!IsKnockedBack) return;
        
        // Apply friction
        _knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, Friction * (float)delta);
        
        // Apply to body
        _body.Velocity = _knockbackVelocity;
        _body.MoveAndSlide();
    }
}
```

---

## 🛡️ Defensive Mechanics

### Invincibility Frames (I-Frames)

```csharp
public void SetInvincible(float duration)
{
    IsInvincible = true;
    
    // Visual feedback (flashing)
    var tween = CreateTween().SetLoops((int)(duration / 0.1f));
    tween.TweenProperty(_sprite, "modulate:a", 0.3f, 0.05f);
    tween.TweenProperty(_sprite, "modulate:a", 1f, 0.05f);
    
    GetTree().CreateTimer(duration).Timeout += () => {
        IsInvincible = false;
        _sprite.Modulate = Colors.White;
    };
}
```

### Blocking

```csharp
public float ProcessDamage(ref DamageInfo info)
{
    if (IsBlocking)
    {
        // Reduce damage
        info.BaseDamage *= (1f - BlockReduction);  // e.g., 0.7 = 30% damage
        
        // Still take stamina/poise damage
        StaminaCurrent -= info.BaseDamage * 0.5f;
        
        // Break block if out of stamina
        if (StaminaCurrent <= 0)
        {
            IsBlocking = false;
            EmitSignal(SignalName.GuardBroken);
        }
    }
    
    // Continue with normal damage processing...
}
```

### Parrying

```csharp
public partial class ParryState : State
{
    private float _parryWindow = 0.2f;  // Timing window
    private float _timer;
    
    public override void Enter()
    {
        _timer = _parryWindow;
        _player.IsParrying = true;
        _player.Sprite.Play("parry");
    }
    
    public override void Exit()
    {
        _player.IsParrying = false;
    }
    
    public override void PhysicsUpdate(double delta)
    {
        _timer -= (float)delta;
        
        if (_timer <= 0)
        {
            // Parry window ended, go to block or idle
            Machine.ChangeState("Block");
        }
    }
}

// In CombatStats
public float ProcessDamage(ref DamageInfo info)
{
    if (IsParrying)
    {
        EmitSignal(SignalName.ParrySuccessful);
        // No damage taken, enemy gets staggered
        return 0f;
    }
    // ...
}
```

---

## 🎯 Combat Design Tips

### 1. Attack Commitment
Don't let players cancel attacks. Commitment = meaningful choices.

```csharp
// In Player state machine
case State.Attacking:
    // Can't move, can't dodge, can't cancel
    Velocity = Vector2.Zero;
    // Just wait for attack to finish
    break;
```

### 2. Telegraphing
Enemies should telegraph attacks so players can react.

```csharp
// Enemy attack state
public override void Enter()
{
    // Play wind-up animation (gives player time to react)
    _enemy.Sprite.Play("attack_windup");
    _timer = WindUpDuration;
    _hasAttacked = false;
}

public override void PhysicsUpdate(double delta)
{
    _timer -= delta;
    
    if (_timer <= 0 && !_hasAttacked)
    {
        // NOW deal damage
        _enemy.PerformAttack();
        _hasAttacked = true;
        _timer = RecoveryDuration;
    }
    else if (_hasAttacked && _timer <= 0)
    {
        Machine.ChangeState("Idle");
    }
}
```

### 3. Sound Design
Every hit needs satisfying audio:

```csharp
// In Hitbox when hit confirmed
_audioPlayer.Stream = HitSounds[GD.RandRange(0, HitSounds.Length - 1)];
_audioPlayer.PitchScale = (float)GD.RandRange(0.9, 1.1);  // Slight variation
_audioPlayer.Play();
```

### 4. Recovery Frames = Punishment Windows
After an attack, the attacker is vulnerable:

```csharp
// Enemy attack has long recovery
[Export] public float RecoveryTime = 0.8f;  // Player can punish!
```

---

## 📋 Combat System Checklist

- [ ] DamageInfo struct for damage data
- [ ] CombatStats component for health/defense
- [ ] Hitbox component for dealing damage
- [ ] Hurtbox component for receiving damage
- [ ] Weapon resource for weapon data
- [ ] Knockback system
- [ ] Screen shake
- [ ] Hitstop
- [ ] Flash on hit
- [ ] I-frames on dodge
- [ ] Damage numbers (optional)
- [ ] Sound effects

---

**Next Chapter**: [09 - Enemy AI](09_ENEMY_AI.md)

