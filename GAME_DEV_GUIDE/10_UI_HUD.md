# Chapter 10: UI & HUD

> **Goal**: Create functional, attractive game interfaces.

---

## 📐 UI Architecture

### Layer Structure

```
Game Scene
├── World (Node2D)          ← Game content
│   ├── Level
│   ├── Player
│   └── Enemies
├── UI (CanvasLayer, Layer=1)    ← HUD, in-game UI
│   ├── HealthBar
│   ├── SkillIcons
│   └── Minimap
├── PauseMenu (CanvasLayer, Layer=2)  ← Pause menu
│   └── PausePanel
└── DialogUI (CanvasLayer, Layer=3)   ← Dialogs on top
    └── DialogBox
```

**CanvasLayer** ensures UI renders on top of game and isn't affected by camera.

---

## 🎯 Common UI Nodes

| Node | Use For |
|------|---------|
| `Control` | Base UI node |
| `Label` | Text display |
| `RichTextLabel` | Formatted text, BBCode |
| `Button` | Clickable buttons |
| `TextureRect` | UI images |
| `ProgressBar` | Health bars, loading |
| `TextureProgressBar` | Stylized bars with textures |
| `HBoxContainer` | Horizontal layout |
| `VBoxContainer` | Vertical layout |
| `GridContainer` | Grid layout (inventory) |
| `MarginContainer` | Adds padding |
| `Panel` | Background panel |

---

## ❤️ Health Bar

### Simple ProgressBar

```csharp
public partial class HealthBar : ProgressBar
{
    public void UpdateHealth(float current, float max)
    {
        MaxValue = max;
        Value = current;
    }
}
```

### Custom TextureProgressBar

```csharp
public partial class HealthBar : Control
{
    [Export] public TextureProgressBar FillBar;
    [Export] public Label HealthText;
    
    public void UpdateHealth(float current, float max)
    {
        FillBar.MaxValue = max;
        FillBar.Value = current;
        
        if (HealthText != null)
            HealthText.Text = $"{(int)current}/{(int)max}";
    }
    
    // Smooth animation
    public void AnimateHealth(float current, float max)
    {
        FillBar.MaxValue = max;
        
        var tween = CreateTween();
        tween.TweenProperty(FillBar, "value", current, 0.3f)
             .SetEase(Tween.EaseType.Out);
    }
}
```

### World-Space Health Bar (Above Enemy)

```csharp
public partial class WorldHealthBar : Node2D
{
    [Export] public Vector2 Size = new Vector2(32, 4);
    [Export] public Vector2 Offset = new Vector2(0, -20);
    
    private ColorRect _background;
    private ColorRect _fill;
    private float _displayedHealth = 1f;
    
    public override void _Ready()
    {
        Position = Offset;
        
        _background = new ColorRect();
        _background.Size = Size;
        _background.Position = -Size / 2;
        _background.Color = new Color(0.2f, 0.2f, 0.2f);
        AddChild(_background);
        
        _fill = new ColorRect();
        _fill.Size = Size;
        _fill.Position = -Size / 2;
        _fill.Color = new Color(0.2f, 0.8f, 0.2f);
        AddChild(_fill);
    }
    
    public void SetHealth(float current, float max)
    {
        float percent = max > 0 ? current / max : 0;
        _fill.Size = new Vector2(Size.X * percent, Size.Y);
    }
}
```

---

## 🎮 Skill Cooldown Icons

```csharp
public partial class SkillIcon : Control
{
    [Export] public TextureRect Icon;
    [Export] public TextureProgressBar CooldownOverlay;
    [Export] public Label Hotkey;
    
    public void StartCooldown(float duration)
    {
        CooldownOverlay.Value = 100;
        
        var tween = CreateTween();
        tween.TweenProperty(CooldownOverlay, "value", 0f, duration);
    }
    
    public void SetIcon(Texture2D texture)
    {
        Icon.Texture = texture;
    }
}
```

---

## 📋 Inventory Grid

```csharp
public partial class InventoryUI : Control
{
    [Export] public GridContainer Grid;
    [Export] public PackedScene SlotScene;
    [Export] public int Columns = 5;
    [Export] public int Rows = 4;
    
    private InventorySlot[,] _slots;
    
    public override void _Ready()
    {
        Grid.Columns = Columns;
        _slots = new InventorySlot[Columns, Rows];
        
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                var slot = SlotScene.Instantiate<InventorySlot>();
                Grid.AddChild(slot);
                _slots[x, y] = slot;
                slot.SlotClicked += () => OnSlotClicked(x, y);
            }
        }
    }
    
    public void SetItem(int x, int y, Item item)
    {
        _slots[x, y].SetItem(item);
    }
}
```

---

## 💬 Dialog Box

```csharp
public partial class DialogBox : Control
{
    [Export] public RichTextLabel TextLabel;
    [Export] public Label SpeakerLabel;
    [Export] public float CharactersPerSecond = 30f;
    
    private string _fullText;
    private float _visibleChars;
    private bool _isTyping;
    
    public async void ShowDialog(string speaker, string text)
    {
        Visible = true;
        SpeakerLabel.Text = speaker;
        _fullText = text;
        _visibleChars = 0;
        _isTyping = true;
        TextLabel.Text = "";
        TextLabel.VisibleCharacters = 0;
        
        // Type out text
        while (_visibleChars < _fullText.Length && _isTyping)
        {
            _visibleChars += CharactersPerSecond * (float)GetProcessDeltaTime();
            TextLabel.Text = _fullText;
            TextLabel.VisibleCharacters = (int)_visibleChars;
            await ToSignal(GetTree(), "process_frame");
        }
        
        TextLabel.VisibleCharacters = -1;  // Show all
        _isTyping = false;
    }
    
    public void Skip()
    {
        if (_isTyping)
        {
            _isTyping = false;
            TextLabel.VisibleCharacters = -1;
        }
        else
        {
            Hide();
        }
    }
}
```

---

## 📱 Responsive UI

### Anchors & Margins

```
Anchor Presets:
┌─────────────────────────────────────┐
│ TopLeft    TopCenter    TopRight    │
│                                     │
│ CenterLeft   Center   CenterRight   │
│                                     │
│ BottomLeft BottomCenter BottomRight │
└─────────────────────────────────────┘

FullRect = Stretches to fill parent
```

In Inspector: Control → Layout → Anchors Preset

### In Code

```csharp
// Set anchor preset
control.SetAnchorsPreset(Control.LayoutPreset.BottomRight);

// Manual anchors (0=left/top, 1=right/bottom)
control.AnchorLeft = 0;
control.AnchorRight = 1;  // Stretch horizontally
control.AnchorTop = 0;
control.AnchorBottom = 0; // Stick to top
```

---

## 🎨 UI Styling

### Theme Resources

Create a Theme resource for consistent styling:

1. Create new Theme resource
2. Add type variations (Button, Label, etc.)
3. Set fonts, colors, styleboxes
4. Apply to root Control node (children inherit)

### StyleBox

```csharp
// Create stylebox in code
var stylebox = new StyleBoxFlat();
stylebox.BgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
stylebox.CornerRadiusTopLeft = 5;
stylebox.CornerRadiusTopRight = 5;
stylebox.CornerRadiusBottomLeft = 5;
stylebox.CornerRadiusBottomRight = 5;
stylebox.BorderWidthTop = 2;
stylebox.BorderWidthBottom = 2;
stylebox.BorderWidthLeft = 2;
stylebox.BorderWidthRight = 2;
stylebox.BorderColor = new Color(0.5f, 0.5f, 0.5f);

panel.AddThemeStyleboxOverride("panel", stylebox);
```

---

## 🔊 UI Feedback

### Button Sounds

```csharp
public partial class UIButton : Button
{
    [Export] public AudioStream HoverSound;
    [Export] public AudioStream ClickSound;
    
    private AudioStreamPlayer _audioPlayer;
    
    public override void _Ready()
    {
        _audioPlayer = new AudioStreamPlayer();
        AddChild(_audioPlayer);
        
        MouseEntered += () => PlaySound(HoverSound);
        Pressed += () => PlaySound(ClickSound);
    }
    
    private void PlaySound(AudioStream sound)
    {
        if (sound == null) return;
        _audioPlayer.Stream = sound;
        _audioPlayer.Play();
    }
}
```

### Button Animation

```csharp
public override void _Ready()
{
    MouseEntered += OnHover;
    MouseExited += OnUnhover;
    ButtonDown += OnPress;
    ButtonUp += OnRelease;
}

private void OnHover()
{
    var tween = CreateTween();
    tween.TweenProperty(this, "scale", Vector2.One * 1.1f, 0.1f);
}

private void OnUnhover()
{
    var tween = CreateTween();
    tween.TweenProperty(this, "scale", Vector2.One, 0.1f);
}
```

---

## ⏸️ Pause Menu

```csharp
public partial class PauseMenu : CanvasLayer
{
    [Export] public Control MenuPanel;
    
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;  // Work when paused!
        MenuPanel.Visible = false;
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("pause"))
        {
            TogglePause();
        }
    }
    
    public void TogglePause()
    {
        bool isPaused = !GetTree().Paused;
        GetTree().Paused = isPaused;
        MenuPanel.Visible = isPaused;
        
        Input.MouseMode = isPaused 
            ? Input.MouseModeEnum.Visible 
            : Input.MouseModeEnum.Hidden;
    }
    
    public void OnResumePressed()
    {
        TogglePause();
    }
    
    public void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
```

---

## 📋 UI Checklist

- [ ] CanvasLayer for UI separation
- [ ] Anchor presets for responsive layout
- [ ] Theme resource for consistent styling
- [ ] Sound feedback on buttons
- [ ] Pause menu with ProcessMode.Always
- [ ] Health bar connected to player signals
- [ ] Skill cooldowns update in real-time

---

**Next Chapter**: [11 - Audio](11_AUDIO.md)

