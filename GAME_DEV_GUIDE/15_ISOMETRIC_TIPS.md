# Chapter 15: Isometric & Top-Down Tips

> **Goal**: Master the unique challenges of 2D isometric/top-down games.

---

## 📐 Understanding Isometric Perspective

### True Isometric vs Top-Down

```
TRUE ISOMETRIC (30° angles)      TOP-DOWN / 3/4 VIEW
        ◇                              ┌───┐
       ╱ ╲                             │   │
      ╱   ╲                            └───┘
     ◇     ◇                        Simple overhead
    Tiles are diamonds            Easier to work with
```

Most "isometric" games actually use **3/4 top-down view** (like your game), which is easier:
- Sprites are drawn from a slight angle
- Movement is standard 4/8 direction
- Depth sorting is the main challenge

---

## 🔄 Y-Sorting (Depth Sorting)

The #1 challenge in isometric games: **things at the bottom of the screen should appear in front of things at the top**.

### Method 1: CanvasItem.YSortEnabled

Simplest approach - let Godot handle it:

```
Scene Tree:
YSortRoot (Node2D with Y Sort Enabled = true)
├── Player          (y=100, draws later)
├── Tree            (y=50, draws first)
├── Enemy           (y=80, draws middle)
└── Rock            (y=120, draws last)
```

In Inspector: Select the parent node → CanvasItem → Y Sort Enabled = ✓

**Important**: All children must be direct children of the Y-sorted node!

### Method 2: Custom Z-Index

For more control:

```csharp
public override void _Process(double delta)
{
    // Z-index based on Y position
    ZIndex = Mathf.RoundToInt(GlobalPosition.Y);
}
```

### Method 3: Sort by Feet Position

Sprites are usually centered or drawn from top. For accurate sorting, use the **feet** position:

```csharp
// In Player/Enemy scripts
public override void _Process(double delta)
{
    // Assuming sprite origin is at center
    // Feet are at bottom of sprite
    float feetY = GlobalPosition.Y + _sprite.Texture.GetHeight() / 2;
    ZIndex = Mathf.RoundToInt(feetY);
}
```

### Multi-Tile Objects (Trees, Buildings)

Large objects need special handling:

```
      🌲 (crown)
      │
   ───┼─── Ground Line (use this for sorting)
      │
    Trunk
```

```csharp
// For a tree, sort by its base, not its center
[Export] public float SortingOffset = 32f;  // Pixels from center to base

public override void _Process(double delta)
{
    ZIndex = Mathf.RoundToInt(GlobalPosition.Y + SortingOffset);
}
```

---

## 🚶 Movement in Isometric

### 8-Direction Movement

```csharp
public Vector2 GetMovementInput()
{
    return Input.GetVector("move_left", "move_right", "move_up", "move_down");
}

public override void _PhysicsProcess(double delta)
{
    var input = GetMovementInput();
    
    if (input != Vector2.Zero)
    {
        // Normalize for consistent speed in diagonals
        Velocity = input.Normalized() * MoveSpeed;
        
        // Update facing direction
        UpdateFacingDirection(input);
    }
    else
    {
        Velocity = Vector2.Zero;
    }
    
    MoveAndSlide();
}

private void UpdateFacingDirection(Vector2 input)
{
    // 4-direction facing
    if (Mathf.Abs(input.X) > Mathf.Abs(input.Y))
    {
        _facingDirection = input.X > 0 ? Direction.Right : Direction.Left;
    }
    else
    {
        _facingDirection = input.Y > 0 ? Direction.Down : Direction.Up;
    }
}
```

### 8-Direction with Proper Diagonals

```csharp
public enum Direction8
{
    Right, DownRight, Down, DownLeft, Left, UpLeft, Up, UpRight
}

public Direction8 VectorToDirection8(Vector2 dir)
{
    if (dir == Vector2.Zero) return Direction8.Down;  // Default
    
    float angle = dir.Angle();
    
    // Convert angle to 0-360
    if (angle < 0) angle += Mathf.Tau;
    
    // Each direction covers 45 degrees (Tau/8)
    int index = Mathf.RoundToInt(angle / (Mathf.Tau / 8)) % 8;
    
    return (Direction8)index;
}
```

---

## 🎨 Sprite Direction

### 4-Direction Sprites

```csharp
private void PlayWalkAnimation(Vector2 direction)
{
    if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
    {
        // Horizontal movement
        if (direction.X > 0)
        {
            _sprite.Play("walk_right");
            _sprite.FlipH = false;
        }
        else
        {
            _sprite.Play("walk_right");  // Same animation
            _sprite.FlipH = true;         // Just flipped
        }
    }
    else
    {
        // Vertical movement
        if (direction.Y > 0)
            _sprite.Play("walk_down");
        else
            _sprite.Play("walk_up");
    }
}
```

### 8-Direction Sprites (If you have the art)

```csharp
private static readonly string[] DirectionNames = 
{
    "right", "down_right", "down", "down_left", 
    "left", "up_left", "up", "up_right"
};

private void PlayAnimation(string baseName, Direction8 direction)
{
    string animName = $"{baseName}_{DirectionNames[(int)direction]}";
    _sprite.Play(animName);
}
```

### Shared Animations with Flip

If you only have 5 directions (right, down-right, down, up-right, up):

```csharp
private void PlayDirectionalAnimation(string baseName, Direction8 dir)
{
    bool flip = false;
    string suffix;
    
    switch (dir)
    {
        case Direction8.Right: suffix = "right"; break;
        case Direction8.DownRight: suffix = "down_right"; break;
        case Direction8.Down: suffix = "down"; break;
        case Direction8.DownLeft: suffix = "down_right"; flip = true; break;
        case Direction8.Left: suffix = "right"; flip = true; break;
        case Direction8.UpLeft: suffix = "up_right"; flip = true; break;
        case Direction8.Up: suffix = "up"; break;
        case Direction8.UpRight: suffix = "up_right"; break;
        default: suffix = "down"; break;
    }
    
    _sprite.Play($"{baseName}_{suffix}");
    _sprite.FlipH = flip;
}
```

---

## 🏠 Collision Shapes

### Use Bottom-Only Collision

In isometric, collision should be at the **base/feet**, not the full sprite:

```
WRONG (full sprite collision):      CORRECT (base only):
    ┌─────────┐                         
    │  TREE   │                          🌳
    │ ┌─────┐ │                           
    │ │     │ │                       ┌─────┐
    └─┼─────┼─┘                       │     │  ← Small collision at base
      └─────┘                         └─────┘
```

```csharp
// In _Ready, position collision shape at feet
_collisionShape.Position = new Vector2(0, _sprite.Texture.GetHeight() / 2 - 5);
```

### Collision Shapes for Characters

```
Player:               Enemy:               Obstacle:
     🧍                  👾                    🪨
   ┌───┐               ┌───┐               ┌─────┐
   └───┘               └───┘               └─────┘
  Small oval        Small oval           Larger box
  at feet           at feet              at base
```

---

## 🎯 Mouse Targeting in Isometric

### Getting Mouse Direction

```csharp
public Vector2 GetMouseDirection()
{
    return (GetGlobalMousePosition() - GlobalPosition).Normalized();
}

public Vector2 GetMousePosition()
{
    return GetGlobalMousePosition();
}
```

### Cursor Aim with Direction Snapping

```csharp
public Direction8 GetAimDirection()
{
    var mouseDir = GetMouseDirection();
    return VectorToDirection8(mouseDir);
}
```

### World Position to Grid Position (for tile-based)

```csharp
public Vector2I WorldToGrid(Vector2 worldPos, int tileSize = 16)
{
    return new Vector2I(
        Mathf.FloorToInt(worldPos.X / tileSize),
        Mathf.FloorToInt(worldPos.Y / tileSize)
    );
}

public Vector2 GridToWorld(Vector2I gridPos, int tileSize = 16)
{
    return new Vector2(
        gridPos.X * tileSize + tileSize / 2f,
        gridPos.Y * tileSize + tileSize / 2f
    );
}
```

---

## 🗺️ TileMap Tips

### Sorting Tiles

For TileMap layers that need depth sorting:
- Split into multiple TileMap layers
- Ground layer: No sorting needed
- Object layer: Y-Sort enabled on parent

### Half-Tile Offset (Staggered Isometric)

For true isometric tilemaps:
```
Project Settings → Rendering → 2D → 
  Tile Y Sort → true (if needed)
```

### Autotiling

Use Godot 4's terrain system:
1. Create terrain set in TileSet
2. Define terrain bits
3. Paint with terrain brush

---

## 📷 Camera Tips

### Camera Following

```csharp
public partial class GameCamera : Camera2D
{
    [Export] public NodePath TargetPath;
    [Export] public float SmoothSpeed = 5f;
    [Export] public Vector2 Offset = new Vector2(0, -20);  // Look slightly ahead
    
    private Node2D _target;
    
    public override void _Ready()
    {
        _target = GetNode<Node2D>(TargetPath);
    }
    
    public override void _Process(double delta)
    {
        if (_target == null) return;
        
        var targetPos = _target.GlobalPosition + Offset;
        GlobalPosition = GlobalPosition.Lerp(targetPos, SmoothSpeed * (float)delta);
    }
}
```

### Camera Bounds

```csharp
[Export] public Rect2 CameraBounds;

public override void _Process(double delta)
{
    // ... follow target
    
    // Clamp to bounds
    if (CameraBounds.Size != Vector2.Zero)
    {
        var viewportSize = GetViewportRect().Size / Zoom;
        var halfSize = viewportSize / 2;
        
        GlobalPosition = new Vector2(
            Mathf.Clamp(GlobalPosition.X, CameraBounds.Position.X + halfSize.X, 
                        CameraBounds.End.X - halfSize.X),
            Mathf.Clamp(GlobalPosition.Y, CameraBounds.Position.Y + halfSize.Y, 
                        CameraBounds.End.Y - halfSize.Y)
        );
    }
}
```

---

## 🎨 Visual Polish

### Shadows

Simple drop shadows for characters:

```csharp
// Add a Sprite2D child named "Shadow"
_shadow.Texture = _sprite.Texture;  // Or a simple oval texture
_shadow.Modulate = new Color(0, 0, 0, 0.3f);
_shadow.Position = new Vector2(2, 8);  // Offset
_shadow.ZIndex = -1;  // Behind character
_shadow.Scale = new Vector2(1, 0.5f);  // Squashed
```

### Footstep Effects

```csharp
private float _footstepTimer;
private const float FootstepInterval = 0.3f;

private void HandleFootsteps(double delta)
{
    if (Velocity == Vector2.Zero) return;
    
    _footstepTimer -= (float)delta;
    
    if (_footstepTimer <= 0)
    {
        _footstepTimer = FootstepInterval;
        SpawnFootstepDust();
        PlayFootstepSound();
    }
}
```

---

## 📋 Isometric Checklist

- [ ] Y-Sorting enabled on container nodes
- [ ] Collision shapes at feet/base of sprites
- [ ] 4 or 8 direction sprites with flip support
- [ ] Proper sprite origin (center-bottom recommended)
- [ ] Large objects have sorting offset
- [ ] Camera smoothly follows player
- [ ] Mouse direction calculated correctly
- [ ] Drop shadows for visual grounding

---

**Next Chapter**: [16 - Common Patterns](16_COMMON_PATTERNS.md)

