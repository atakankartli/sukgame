# Chapter 04: Input System

> **Goal**: Handle player input cleanly and support multiple control schemes.

---

## ⚙️ Input Map Setup

Configure inputs in: **Project → Project Settings → Input Map**

### Recommended Actions

```
# Movement
move_left       → A, Left Arrow, D-Pad Left
move_right      → D, Right Arrow, D-Pad Right  
move_up         → W, Up Arrow, D-Pad Up
move_down       → S, Down Arrow, D-Pad Down

# Combat
attack          → Left Mouse Button, Gamepad X/Square
skill1          → 1, Gamepad Y/Triangle
skill2          → 2, Gamepad B/Circle
dodge           → Space, Shift, Gamepad A/Cross
block           → Right Mouse Button, Gamepad LT

# UI
pause           → Escape, Gamepad Start
inventory       → Tab, I, Gamepad Select
interact        → E, F, Gamepad A/Cross

# Debug (optional)
debug_toggle    → F3
```

---

## 🎮 Reading Input

### Action Checks

```csharp
// Just pressed this frame (single trigger)
if (Input.IsActionJustPressed("attack"))
{
    Attack();
}

// Being held down
if (Input.IsActionPressed("block"))
{
    Block();
}

// Just released this frame
if (Input.IsActionJustReleased("jump"))
{
    ReleaseJump();
}

// Analog value (for triggers/sticks)
float pressure = Input.GetActionStrength("accelerate");  // 0.0 to 1.0
```

### Movement Input

```csharp
// Best way: GetVector handles normalization
Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
// Returns normalized vector when magnitude > 1
// Handles deadzone for analog sticks

// Apply to movement
Velocity = input * MoveSpeed;
MoveAndSlide();
```

### Mouse Input

```csharp
// Mouse position in viewport
Vector2 screenPos = GetViewport().GetMousePosition();

// Mouse position in world (what you usually want)
Vector2 worldPos = GetGlobalMousePosition();

// Direction from player to mouse
Vector2 aimDirection = (GetGlobalMousePosition() - GlobalPosition).Normalized();

// Mouse button states
if (Input.IsMouseButtonPressed(MouseButton.Left))
{
    // Holding left click
}
```

---

## 🔄 Input Processing Methods

### _Input() - All Input Events

```csharp
public override void _Input(InputEvent @event)
{
    // Fires for EVERY input event
    if (@event.IsActionPressed("pause"))
    {
        TogglePause();
        GetViewport().SetInputAsHandled();  // Prevent propagation
    }
}
```

### _UnhandledInput() - Unhandled Events Only

```csharp
public override void _UnhandledInput(InputEvent @event)
{
    // Only fires if no other node handled the input
    // Good for game controls that shouldn't fire when UI is focused
    if (@event.IsActionPressed("attack"))
    {
        Attack();
    }
}
```

### Checking in _Process/_PhysicsProcess

```csharp
public override void _PhysicsProcess(double delta)
{
    // Check every frame - good for held actions
    if (Input.IsActionPressed("move_right"))
    {
        Velocity.X = MoveSpeed;
    }
}
```

### When to Use Each

| Method | Use For |
|--------|---------|
| `_Input` | UI, pause menu, always-active inputs |
| `_UnhandledInput` | Game controls, movement, combat |
| `_Process` polling | Held buttons, continuous input |

---

## 🕹️ Input Buffering

Buffer inputs so button presses aren't lost during animations:

```csharp
private float _attackBuffer;
private float _dodgeBuffer;
private const float BufferTime = 0.15f;  // 150ms buffer window

public override void _Process(double delta)
{
    // Decrease buffers
    _attackBuffer = Mathf.Max(0, _attackBuffer - (float)delta);
    _dodgeBuffer = Mathf.Max(0, _dodgeBuffer - (float)delta);
    
    // Set buffers when input detected
    if (Input.IsActionJustPressed("attack"))
        _attackBuffer = BufferTime;
    
    if (Input.IsActionJustPressed("dodge"))
        _dodgeBuffer = BufferTime;
}

private void TryBufferedActions()
{
    // Called when we CAN act (e.g., returning to Idle state)
    if (_attackBuffer > 0)
    {
        _attackBuffer = 0;
        StartAttack();
    }
    else if (_dodgeBuffer > 0)
    {
        _dodgeBuffer = 0;
        StartDodge();
    }
}
```

---

## 📱 Input Device Detection

```csharp
// Check last used device
private bool _usingController;

public override void _Input(InputEvent @event)
{
    if (@event is InputEventJoypadButton || @event is InputEventJoypadMotion)
    {
        _usingController = true;
    }
    else if (@event is InputEventKey || @event is InputEventMouse)
    {
        _usingController = false;
    }
}

// Use for UI prompts
public string GetAttackPrompt()
{
    return _usingController ? "X" : "LMB";
}
```

---

## 🖱️ Mouse Handling

### Cursor Visibility

```csharp
// Hide cursor (for controller/immersive games)
Input.MouseMode = Input.MouseModeEnum.Hidden;

// Capture cursor (for FPS-style camera)
Input.MouseMode = Input.MouseModeEnum.Captured;

// Normal cursor
Input.MouseMode = Input.MouseModeEnum.Visible;

// Confined to window
Input.MouseMode = Input.MouseModeEnum.Confined;
```

### Custom Cursor

```csharp
// In _Ready or when changing cursor
var cursorTexture = GD.Load<Texture2D>("res://assets/ui/cursor.png");
Input.SetCustomMouseCursor(cursorTexture, Input.CursorShape.Arrow, new Vector2(16, 16));

// Hotspot is the click point (center for crosshair, tip for arrow)
```

---

## 🎯 Aim Direction Helpers

### 4-Direction from Input

```csharp
public enum Direction { Right, Down, Left, Up }

public Direction GetFacingDirection(Vector2 input)
{
    if (input == Vector2.Zero) return _lastDirection;
    
    if (Mathf.Abs(input.X) > Mathf.Abs(input.Y))
    {
        _lastDirection = input.X > 0 ? Direction.Right : Direction.Left;
    }
    else
    {
        _lastDirection = input.Y > 0 ? Direction.Down : Direction.Up;
    }
    
    return _lastDirection;
}
```

### 8-Direction from Input

```csharp
public enum Direction8 
{ 
    Right, DownRight, Down, DownLeft, 
    Left, UpLeft, Up, UpRight 
}

public Direction8 GetDirection8(Vector2 input)
{
    if (input == Vector2.Zero) return _lastDirection8;
    
    float angle = input.Angle();
    if (angle < 0) angle += Mathf.Tau;  // Normalize to 0-2π
    
    int index = Mathf.RoundToInt(angle / (Mathf.Tau / 8)) % 8;
    _lastDirection8 = (Direction8)index;
    
    return _lastDirection8;
}
```

### Mouse vs Movement for Aiming

```csharp
public Vector2 GetAimDirection()
{
    // Prefer mouse if using keyboard/mouse
    if (!_usingController)
    {
        return (GetGlobalMousePosition() - GlobalPosition).Normalized();
    }
    
    // Use right stick for controller
    Vector2 rightStick = Input.GetVector("aim_left", "aim_right", "aim_up", "aim_down");
    if (rightStick.Length() > 0.3f)  // Deadzone
    {
        return rightStick.Normalized();
    }
    
    // Fallback to movement direction
    return _facingDirection;
}
```

---

## ⏸️ Pause Handling

```csharp
public partial class PauseManager : Node
{
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("pause"))
        {
            TogglePause();
        }
    }
    
    public void TogglePause()
    {
        GetTree().Paused = !GetTree().Paused;
        
        // Show/hide pause menu
        _pauseMenu.Visible = GetTree().Paused;
        
        // Mouse visibility
        Input.MouseMode = GetTree().Paused 
            ? Input.MouseModeEnum.Visible 
            : Input.MouseModeEnum.Hidden;
    }
}
```

**Important**: Pause menu nodes need `Process Mode = Always` to work when paused!

---

## 🔧 Input Remapping (Runtime)

```csharp
public void RemapAction(string action, InputEvent newEvent)
{
    // Clear existing events
    InputMap.ActionEraseEvents(action);
    
    // Add new event
    InputMap.ActionAddEvent(action, newEvent);
    
    // Save to config (implement SaveInputConfig)
    SaveInputConfig();
}

// Example: Remap attack to different key
var keyEvent = new InputEventKey();
keyEvent.Keycode = Key.J;
RemapAction("attack", keyEvent);
```

---

## 📋 Input Best Practices

### DO:
```csharp
// ✅ Use action names, not raw keys
if (Input.IsActionJustPressed("attack"))

// ✅ Normalize movement
Vector2 input = Input.GetVector(...);  // Auto-normalized

// ✅ Buffer important inputs
if (_attackBuffer > 0) Attack();

// ✅ Check input validity
if (direction != Vector2.Zero)
```

### DON'T:
```csharp
// ❌ Hardcode keys
if (Input.IsKeyPressed(Key.Space))

// ❌ Forget diagonal normalization
Velocity = new Vector2(inputX, inputY) * Speed;  // Faster diagonally!

// ❌ Process input in wrong method
public override void _Ready()
{
    if (Input.IsActionPressed("attack"))  // Only runs once!
}
```

---

## 📋 Input System Checklist

- [ ] All actions defined in Input Map
- [ ] Multiple bindings per action (keyboard + controller)
- [ ] Movement uses GetVector (normalized)
- [ ] Input buffering for responsive combat
- [ ] Pause works with Process Mode set correctly
- [ ] Cursor handling (hide, custom, etc.)
- [ ] Device detection for UI prompts

---

**Next Chapter**: [05 - Movement & Physics](05_MOVEMENT_PHYSICS.md)

