# Chapter 07: State Machines

> **Goal**: Build predictable, debuggable behavior with state machines.

---

## 🤔 Why State Machines?

Without state machines, code becomes spaghetti:

```csharp
// BAD: Flag-based chaos
private bool _isAttacking;
private bool _isDashing;
private bool _isStunned;
private bool _isDead;

public override void _PhysicsProcess(double delta)
{
    if (_isDead) return;
    if (_isStunned && !_isDashing) return;
    if (_isAttacking && !_isStunned)
    {
        // What happens if stunned while attacking?
        // What if we dash while attacking?
        // This gets messy fast...
    }
}
```

With state machines, logic is clean:

```csharp
// GOOD: State machine
private enum State { Idle, Walking, Attacking, Dashing, Stunned, Dead }
private State _currentState = State.Idle;

public override void _PhysicsProcess(double delta)
{
    switch (_currentState)
    {
        case State.Idle:     IdleState(delta); break;
        case State.Walking:  WalkingState(delta); break;
        case State.Attacking: AttackingState(delta); break;
        // Each state handles its own logic
    }
}
```

---

## 📊 State Machine Patterns

### Pattern 1: Enum + Switch (Simple)

Best for: Player with few states, simple enemies

```csharp
public partial class Player : CharacterBody2D
{
    private enum State { Idle, Walking, Attacking, Dashing, Dead }
    private State _currentState = State.Idle;
    private double _stateTimer;
    
    public override void _PhysicsProcess(double delta)
    {
        switch (_currentState)
        {
            case State.Idle:
                HandleIdleState();
                break;
            case State.Walking:
                HandleWalkingState();
                break;
            case State.Attacking:
                HandleAttackingState(delta);
                break;
            case State.Dashing:
                HandleDashingState(delta);
                break;
            case State.Dead:
                // Do nothing
                break;
        }
    }
    
    private void HandleIdleState()
    {
        // Check for transitions
        var input = GetMovementInput();
        if (input != Vector2.Zero)
        {
            ChangeState(State.Walking);
            return;
        }
        
        if (Input.IsActionJustPressed("attack"))
        {
            ChangeState(State.Attacking);
            return;
        }
        
        // State logic
        _sprite.Play("idle");
        Velocity = Vector2.Zero;
    }
    
    private void HandleWalkingState()
    {
        var input = GetMovementInput();
        
        // Transitions
        if (input == Vector2.Zero)
        {
            ChangeState(State.Idle);
            return;
        }
        
        if (Input.IsActionJustPressed("attack"))
        {
            ChangeState(State.Attacking);
            return;
        }
        
        if (Input.IsActionJustPressed("dash"))
        {
            ChangeState(State.Dashing);
            return;
        }
        
        // State logic
        Velocity = input * MoveSpeed;
        PlayWalkAnimation(input);
        MoveAndSlide();
    }
    
    private void HandleAttackingState(double delta)
    {
        _stateTimer -= delta;
        Velocity = Vector2.Zero;  // Can't move while attacking
        
        // Transition when timer expires
        if (_stateTimer <= 0)
        {
            ChangeState(State.Idle);
            return;
        }
    }
    
    private void HandleDashingState(double delta)
    {
        _stateTimer -= delta;
        
        if (_stateTimer <= 0)
        {
            ChangeState(State.Idle);
            return;
        }
        
        // Dash movement
        Velocity = _dashDirection * DashSpeed;
        MoveAndSlide();
    }
    
    private void ChangeState(State newState)
    {
        // Exit current state
        switch (_currentState)
        {
            case State.Dashing:
                _combatStats?.ClearInvincibility();
                break;
            case State.Attacking:
                _weapon?.EndAttack();
                break;
        }
        
        // Enter new state
        _currentState = newState;
        
        switch (newState)
        {
            case State.Attacking:
                _stateTimer = AttackDuration;
                _weapon?.StartAttack();
                break;
            case State.Dashing:
                _stateTimer = DashDuration;
                _dashDirection = GetMovementInput().Normalized();
                _combatStats?.SetInvincible(DashDuration);
                break;
            case State.Dead:
                _sprite.Play("die");
                break;
        }
        
        GD.Print($"State changed to: {newState}");
    }
}
```

---

### Pattern 2: State Objects (Scalable)

Best for: Complex characters, reusable states, multiple enemies

#### Base State Class

```csharp
// scripts/StateMachine/State.cs
using Godot;

public abstract partial class State : Node
{
    // Reference to the state machine
    protected StateMachine Machine;
    
    // Called when state machine initializes
    public virtual void Init(StateMachine machine)
    {
        Machine = machine;
    }
    
    // Called when entering this state
    public virtual void Enter() { }
    
    // Called when exiting this state
    public virtual void Exit() { }
    
    // Called every _Process frame
    public virtual void Update(double delta) { }
    
    // Called every _PhysicsProcess frame
    public virtual void PhysicsUpdate(double delta) { }
    
    // Called for input handling
    public virtual void HandleInput(InputEvent @event) { }
}
```

#### State Machine

```csharp
// scripts/StateMachine/StateMachine.cs
using Godot;
using System.Collections.Generic;

public partial class StateMachine : Node
{
    [Export] public NodePath InitialState;
    
    public State CurrentState { get; private set; }
    private Dictionary<string, State> _states = new();
    
    public override void _Ready()
    {
        // Collect all child states
        foreach (var child in GetChildren())
        {
            if (child is State state)
            {
                _states[child.Name] = state;
                state.Init(this);
            }
        }
        
        // Start with initial state
        if (InitialState != null && !InitialState.IsEmpty)
        {
            CurrentState = GetNode<State>(InitialState);
            CurrentState.Enter();
        }
    }
    
    public override void _Process(double delta)
    {
        CurrentState?.Update(delta);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        CurrentState?.PhysicsUpdate(delta);
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        CurrentState?.HandleInput(@event);
    }
    
    public void ChangeState(string stateName)
    {
        if (!_states.ContainsKey(stateName))
        {
            GD.PrintErr($"State '{stateName}' not found!");
            return;
        }
        
        CurrentState?.Exit();
        CurrentState = _states[stateName];
        CurrentState.Enter();
        
        GD.Print($"Changed to state: {stateName}");
    }
    
    public void ChangeState(State state)
    {
        CurrentState?.Exit();
        CurrentState = state;
        CurrentState.Enter();
    }
}
```

#### Example States

```csharp
// scripts/Player/States/PlayerIdleState.cs
public partial class PlayerIdleState : State
{
    private Player _player;
    
    public override void Init(StateMachine machine)
    {
        base.Init(machine);
        _player = machine.GetParent<Player>();
    }
    
    public override void Enter()
    {
        _player.Sprite.Play("idle");
        _player.Velocity = Vector2.Zero;
    }
    
    public override void PhysicsUpdate(double delta)
    {
        // Check transitions
        var input = _player.GetMovementInput();
        
        if (input != Vector2.Zero)
        {
            Machine.ChangeState("Walk");
            return;
        }
        
        if (Input.IsActionJustPressed("attack"))
        {
            Machine.ChangeState("Attack");
            return;
        }
    }
}
```

```csharp
// scripts/Player/States/PlayerWalkState.cs
public partial class PlayerWalkState : State
{
    private Player _player;
    
    public override void Init(StateMachine machine)
    {
        base.Init(machine);
        _player = machine.GetParent<Player>();
    }
    
    public override void Enter()
    {
        // Walking animation is handled in Update based on direction
    }
    
    public override void PhysicsUpdate(double delta)
    {
        var input = _player.GetMovementInput();
        
        // Transitions
        if (input == Vector2.Zero)
        {
            Machine.ChangeState("Idle");
            return;
        }
        
        if (Input.IsActionJustPressed("attack"))
        {
            Machine.ChangeState("Attack");
            return;
        }
        
        if (Input.IsActionJustPressed("dash"))
        {
            Machine.ChangeState("Dash");
            return;
        }
        
        // Movement
        _player.Velocity = input * _player.MoveSpeed;
        _player.PlayWalkAnimation(input);
        _player.MoveAndSlide();
    }
}
```

```csharp
// scripts/Player/States/PlayerAttackState.cs
public partial class PlayerAttackState : State
{
    private Player _player;
    private double _timer;
    
    public override void Init(StateMachine machine)
    {
        base.Init(machine);
        _player = machine.GetParent<Player>();
    }
    
    public override void Enter()
    {
        _timer = _player.AttackDuration;
        _player.Velocity = Vector2.Zero;
        _player.MeleeWeapon.Attack(_player.FacingDirection);
    }
    
    public override void Exit()
    {
        _player.MeleeWeapon.CancelAttack();
    }
    
    public override void PhysicsUpdate(double delta)
    {
        _timer -= delta;
        
        if (_timer <= 0)
        {
            Machine.ChangeState("Idle");
        }
    }
}
```

#### Scene Structure

```
Player (CharacterBody2D)
├── AnimatedSprite2D
├── CollisionShape2D
├── StateMachine (StateMachine.cs)
│   ├── Idle (PlayerIdleState.cs)
│   ├── Walk (PlayerWalkState.cs)
│   ├── Attack (PlayerAttackState.cs)
│   ├── Dash (PlayerDashState.cs)
│   └── Dead (PlayerDeadState.cs)
└── ...
```

---

## 📈 State Transition Diagrams

Always design your state machine on paper first:

```
                    ┌──────────────────────────────┐
                    │                              │
                    ▼                              │
              ┌─────────┐    input!=0    ┌────────┴──┐
              │  IDLE   │───────────────►│  WALKING  │
              └────┬────┘                └─────┬─────┘
                   │                           │
                   │ attack                    │ attack
                   ▼                           ▼
              ┌─────────────────────────────────────┐
              │             ATTACKING               │
              └─────────────────┬───────────────────┘
                                │ timer done
                                ▼
                           (back to IDLE)
```

```
STAGGER (from any state except Dead):
    Any State ──► hit + poise broken ──► STAGGER ──► timer ──► IDLE

DEAD (from any state):
    Any State ──► health <= 0 ──► DEAD (no exit)
```

---

## 🎯 State Machine Best Practices

### 1. Define Clear Entry/Exit

```csharp
public override void Enter()
{
    // Set up everything this state needs
    _timer = Duration;
    _sprite.Play("attack");
    _hitbox.Enable();
}

public override void Exit()
{
    // Clean up everything
    _hitbox.Disable();
    _sprite.SpeedScale = 1.0f;
}
```

### 2. States Don't Know About Each Other

```csharp
// BAD: State knows about other states
if (_timer <= 0)
{
    _idleState.Enter();  // Direct reference to another state
}

// GOOD: State tells machine to change
if (_timer <= 0)
{
    Machine.ChangeState("Idle");  // Machine handles the transition
}
```

### 3. Use State for Conditional Logic

```csharp
// BAD: Flags everywhere
if (Input.IsActionPressed("attack") && !_isStunned && !_isDead && !_isAttacking)

// GOOD: Each state handles its own logic
// In AttackState, we simply don't check for attack input
// The transition TO attack is handled in Idle/Walk states
```

### 4. Debug Your States

```csharp
// Add debug output
public void ChangeState(string stateName)
{
    GD.Print($"[{Time.GetTicksMsec()}] {_currentState?.Name} -> {stateName}");
    // ... change state logic
}

// Or show on screen
public override void _Process(double delta)
{
    DebugLabel.Text = $"State: {CurrentState?.Name}";
}
```

---

## 🔀 Hierarchical State Machines (Advanced)

For complex characters, use nested states:

```
Grounded (parent state)
├── Idle
├── Walk
├── Run
└── Crouch
    ├── CrouchIdle
    └── CrouchWalk

Airborne (parent state)
├── Jump
├── Fall
└── AirAttack

Combat (parent state)
├── LightAttack1
├── LightAttack2
├── LightAttack3
└── HeavyAttack
```

Parent states can handle common logic:
- Grounded: gravity doesn't apply
- Airborne: gravity applies, can't attack normally
- Combat: can't move, can transition to next combo

---

## 🎮 Enemy AI States Example

```csharp
public partial class EnemyIdleState : State
{
    private Enemy _enemy;
    private double _idleTimer;
    
    public override void Enter()
    {
        _enemy.Sprite.Play("idle");
        _idleTimer = GD.RandRange(1.0, 3.0);  // Random wait
    }
    
    public override void PhysicsUpdate(double delta)
    {
        // Check for player detection
        if (_enemy.CanSeePlayer())
        {
            Machine.ChangeState("Chase");
            return;
        }
        
        // Random patrol
        _idleTimer -= delta;
        if (_idleTimer <= 0)
        {
            Machine.ChangeState("Patrol");
        }
    }
}

public partial class EnemyChaseState : State
{
    private Enemy _enemy;
    
    public override void PhysicsUpdate(double delta)
    {
        if (!_enemy.CanSeePlayer())
        {
            Machine.ChangeState("Search");
            return;
        }
        
        float distance = _enemy.DistanceToPlayer();
        
        if (distance < _enemy.AttackRange)
        {
            Machine.ChangeState("Attack");
            return;
        }
        
        // Move toward player
        var direction = _enemy.DirectionToPlayer();
        _enemy.Velocity = direction * _enemy.ChaseSpeed;
        _enemy.MoveAndSlide();
    }
}
```

---

**Next Chapter**: [08 - Combat System](08_COMBAT_SYSTEM.md)

