# Chapter 06: Animation System

> **Goal**: Create smooth, responsive animations that enhance gameplay.

---

## 🎭 AnimatedSprite2D vs AnimationPlayer

| Feature | AnimatedSprite2D | AnimationPlayer |
|---------|------------------|-----------------|
| Use case | Simple sprite animations | Complex multi-property animations |
| Ease | Very easy | More setup |
| Flexibility | Sprites only | Any property, multiple nodes |
| Frame events | AnimationFinished signal | Method calls, signals at keyframes |

**Recommendation**: Use `AnimatedSprite2D` for character animations, `AnimationPlayer` for cutscenes/UI.

---

## 🖼️ AnimatedSprite2D Setup

### Creating Animations

1. Add `AnimatedSprite2D` node
2. Inspector → Sprite Frames → New SpriteFrames
3. Click to edit the SpriteFrames
4. Bottom panel opens → Add animations

### From Sprite Sheet

1. In SpriteFrames panel, click grid icon (Add frames from Sprite Sheet)
2. Select your sprite sheet
3. Set grid size (e.g., 16x32 for each frame)
4. Select frames for this animation
5. Add to animation

### Animation Properties

```
Animation Panel:
├── Animation Name: "walk_right"
├── FPS: 10 (frames per second)
├── Loop: ✓ (for walk, idle) or ✗ (for attack, die)
└── Frames: [frame0, frame1, frame2, ...]
```

---

## 🎮 Playing Animations in Code

### Basic Playback

```csharp
private AnimatedSprite2D _sprite;

public override void _Ready()
{
    _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
}

// Play animation
_sprite.Play("walk_right");

// Play from beginning (even if already playing)
_sprite.Play("attack");
_sprite.Frame = 0;

// Stop animation
_sprite.Stop();

// Pause at current frame
_sprite.Pause();

// Check current animation
if (_sprite.Animation == "idle")
{
    // Currently idling
}

// Check if animation exists
if (_sprite.SpriteFrames.HasAnimation("die"))
{
    _sprite.Play("die");
}
```

### Animation Speed

```csharp
// Speed multiplier (1.0 = normal)
_sprite.SpeedScale = 2.0f;  // Double speed
_sprite.SpeedScale = 0.5f;  // Half speed

// Play backwards
_sprite.SpeedScale = -1.0f;
```

### Animation Events

```csharp
public override void _Ready()
{
    _sprite.AnimationFinished += OnAnimationFinished;
    _sprite.FrameChanged += OnFrameChanged;
}

private void OnAnimationFinished()
{
    if (_sprite.Animation == "attack")
    {
        // Attack animation done, return to idle
        _sprite.Play("idle");
    }
    else if (_sprite.Animation == "die")
    {
        QueueFree();
    }
}

private void OnFrameChanged()
{
    // Called every frame change
    if (_sprite.Animation == "attack" && _sprite.Frame == 3)
    {
        // Frame 3 of attack = activate hitbox
        _hitbox.Activate();
    }
}
```

---

## 🔄 State-Based Animation

### Simple Approach

```csharp
private void UpdateAnimation()
{
    switch (_currentState)
    {
        case State.Idle:
            _sprite.Play("idle");
            break;
            
        case State.Walking:
            PlayWalkAnimation(_facingDirection);
            break;
            
        case State.Attacking:
            // Attack animation started when entering state
            break;
            
        case State.Dead:
            _sprite.Play("die");
            break;
    }
}

private void PlayWalkAnimation(Vector2 direction)
{
    if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
    {
        // Horizontal movement
        _sprite.Play("walk_right");
        _sprite.FlipH = direction.X < 0;
    }
    else
    {
        // Vertical movement
        _sprite.Play(direction.Y > 0 ? "walk_down" : "walk_up");
    }
}
```

### Animation Priority

```csharp
private int GetAnimationPriority(string animName)
{
    return animName switch
    {
        "die" => 100,      // Highest - always plays
        "hit" => 80,       // High - interrupts most
        "attack" => 60,    // Medium-high
        "walk" => 20,      // Low
        "idle" => 10,      // Lowest
        _ => 0
    };
}

private void TryPlayAnimation(string animName)
{
    int newPriority = GetAnimationPriority(animName);
    int currentPriority = GetAnimationPriority(_sprite.Animation);
    
    if (newPriority >= currentPriority)
    {
        _sprite.Play(animName);
    }
}
```

---

## 🎬 AnimationPlayer (Advanced)

### When to Use
- Animating multiple properties at once
- Animating multiple nodes together
- Complex UI animations
- Cutscenes
- Method calls at specific times

### Setup

1. Add `AnimationPlayer` node
2. Click AnimationPlayer → Animation panel at bottom
3. Animation → New → Name it
4. Add tracks:
   - Right-click → Add Track → Property Track
   - Select node and property to animate
   - Add keyframes

### Common Tracks

```
Property Track     → Animate any property (position, scale, color)
Method Call Track  → Call methods at specific times
Audio Track        → Play sounds at specific times
Animation Track    → Trigger other animations
```

### Playing in Code

```csharp
private AnimationPlayer _anim;

_anim.Play("fade_in");
_anim.PlayBackwards("fade_in");  // Reverse
_anim.Queue("next_animation");   // Play after current

// Speed
_anim.SpeedScale = 2.0f;

// Signals
_anim.AnimationFinished += (animName) => {
    GD.Print($"Finished: {animName}");
};
```

---

## ⚡ Animation Tips

### Tip 1: Use FlipH Instead of Duplicate Animations

```csharp
// Instead of walk_left and walk_right:
_sprite.Play("walk_right");
_sprite.FlipH = direction.X < 0;

// You only need: walk_right, walk_up, walk_down
// (Maybe walk_up_right for 8-direction)
```

### Tip 2: Frame-Perfect Hitboxes

```csharp
// Option 1: Check frame in FrameChanged
_sprite.FrameChanged += () => {
    bool isActiveFrame = _sprite.Frame >= 2 && _sprite.Frame <= 4;
    _hitbox.Active = isActiveFrame;
};

// Option 2: Use animation signals (in SpriteFrames editor)
// Add method call track that calls ActivateHitbox() on frame 2
```

### Tip 3: Animation Blending (Smooth Transitions)

```csharp
// For AnimationPlayer:
_animationTree.Set("parameters/blend_position", direction);

// For simple cases, crossfade with tween:
var tween = CreateTween();
tween.TweenProperty(_sprite, "modulate:a", 0f, 0.1f);
tween.TweenCallback(Callable.From(() => _sprite.Play("new_anim")));
tween.TweenProperty(_sprite, "modulate:a", 1f, 0.1f);
```

### Tip 4: Squash and Stretch

```csharp
// Jump anticipation
var tween = CreateTween();
tween.TweenProperty(_sprite, "scale", new Vector2(1.2f, 0.8f), 0.1f);
tween.TweenProperty(_sprite, "scale", new Vector2(0.8f, 1.3f), 0.1f);
tween.TweenProperty(_sprite, "scale", Vector2.One, 0.2f);
```

### Tip 5: Landing Impact

```csharp
private void OnLanded()
{
    // Squash on land
    _sprite.Scale = new Vector2(1.3f, 0.7f);
    
    var tween = CreateTween();
    tween.SetEase(Tween.EaseType.Out);
    tween.SetTrans(Tween.TransitionType.Elastic);
    tween.TweenProperty(_sprite, "scale", Vector2.One, 0.3f);
}
```

---

## 🎨 Animation Naming Convention

```
idle                    # Standing still
walk_right             # Walking (flip for left)
walk_up
walk_down
run_right
attack_right           # Attacking
attack_up
attack_down
hit                    # Taking damage
die                    # Death (no loop)
cast                   # Casting spell
dodge_right            # Dodge roll
```

For 8-direction:
```
walk_right
walk_down_right
walk_down
walk_down_left
walk_left
walk_up_left
walk_up
walk_up_right
```

---

## 📋 Animation Checklist

- [ ] Consistent FPS across animations (usually 10-15 for pixel art)
- [ ] Loop setting correct (loop for idle/walk, no loop for attack/die)
- [ ] Flip instead of duplicate left/right animations
- [ ] AnimationFinished connected for non-looping anims
- [ ] Attack animation synced with hitbox timing
- [ ] Death animation triggers QueueFree when done
- [ ] Speed scale reset after attacks

---

**Next Chapter**: [07 - State Machines](07_STATE_MACHINES.md)

