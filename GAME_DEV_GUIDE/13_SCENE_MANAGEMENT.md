# Chapter 13: Scene Management

> **Goal**: Handle level transitions, loading screens, and game flow.

---

## 🔄 Basic Scene Changing

### Simple Change

```csharp
// Change scene immediately
GetTree().ChangeSceneToFile("res://scenes/levels/level2.tscn");

// Or with PackedScene
PackedScene nextLevel = GD.Load<PackedScene>("res://scenes/levels/level2.tscn");
GetTree().ChangeSceneToPacked(nextLevel);
```

### The Problem

Immediate scene changes cause:
- No transition/loading screen
- Frame freeze on complex scenes
- Can't pass data between scenes

---

## 🎬 Scene Manager Singleton

```csharp
// AutoLoad this script (Project Settings → AutoLoad)
public partial class SceneManager : Node
{
    public static SceneManager Instance { get; private set; }
    
    [Signal] public delegate void SceneChangedEventHandler(string sceneName);
    
    private string _currentScene;
    private PackedScene _loadingScreen;
    
    public override void _Ready()
    {
        Instance = this;
        _loadingScreen = GD.Load<PackedScene>("res://scenes/ui/loading_screen.tscn");
        ProcessMode = ProcessModeEnum.Always;  // Work during pause
    }
    
    public async void ChangeScene(string path, bool showLoading = true)
    {
        if (showLoading)
        {
            await ShowLoadingScreen();
        }
        
        // Load the new scene
        var newScene = GD.Load<PackedScene>(path);
        
        // Change scene
        GetTree().ChangeSceneToPacked(newScene);
        
        _currentScene = path;
        EmitSignal(SignalName.SceneChanged, path);
        
        if (showLoading)
        {
            await HideLoadingScreen();
        }
    }
    
    private LoadingScreen _loadingInstance;
    
    private async Task ShowLoadingScreen()
    {
        _loadingInstance = _loadingScreen.Instantiate<LoadingScreen>();
        GetTree().Root.AddChild(_loadingInstance);
        await _loadingInstance.FadeIn();
    }
    
    private async Task HideLoadingScreen()
    {
        if (_loadingInstance != null)
        {
            await _loadingInstance.FadeOut();
            _loadingInstance.QueueFree();
            _loadingInstance = null;
        }
    }
}
```

**Usage:**
```csharp
SceneManager.Instance.ChangeScene("res://scenes/levels/forest.tscn");
```

---

## 📊 Loading Screen

```csharp
public partial class LoadingScreen : CanvasLayer
{
    [Export] public ColorRect Background;
    [Export] public Label LoadingText;
    [Export] public TextureProgressBar ProgressBar;
    
    public override void _Ready()
    {
        Background.Modulate = new Color(1, 1, 1, 0);
    }
    
    public async Task FadeIn()
    {
        var tween = CreateTween();
        tween.TweenProperty(Background, "modulate:a", 1f, 0.3f);
        await ToSignal(tween, "finished");
    }
    
    public async Task FadeOut()
    {
        var tween = CreateTween();
        tween.TweenProperty(Background, "modulate:a", 0f, 0.3f);
        await ToSignal(tween, "finished");
    }
    
    public void SetProgress(float percent)
    {
        ProgressBar.Value = percent * 100;
        LoadingText.Text = $"Loading... {(int)(percent * 100)}%";
    }
}
```

---

## ⏳ Background Loading (Large Scenes)

```csharp
public async void ChangeSceneAsync(string path)
{
    await ShowLoadingScreen();
    
    // Start loading in background
    ResourceLoader.LoadThreadedRequest(path);
    
    // Poll loading progress
    while (true)
    {
        var status = ResourceLoader.LoadThreadedGetStatus(path, new Godot.Collections.Array());
        
        if (status == ResourceLoader.ThreadLoadStatus.Loaded)
        {
            break;
        }
        else if (status == ResourceLoader.ThreadLoadStatus.Failed)
        {
            GD.PrintErr($"Failed to load: {path}");
            return;
        }
        
        // Get progress (0.0 to 1.0)
        var progress = new Godot.Collections.Array();
        ResourceLoader.LoadThreadedGetStatus(path, progress);
        float percent = progress.Count > 0 ? (float)progress[0] : 0f;
        _loadingInstance.SetProgress(percent);
        
        await ToSignal(GetTree(), "process_frame");
    }
    
    // Get the loaded resource
    var packedScene = ResourceLoader.LoadThreadedGet(path) as PackedScene;
    GetTree().ChangeSceneToPacked(packedScene);
    
    await HideLoadingScreen();
}
```

---

## 📦 Passing Data Between Scenes

### Method 1: Singleton/AutoLoad

```csharp
// GameState.cs (AutoLoad)
public partial class GameState : Node
{
    public static GameState Instance { get; private set; }
    
    // Data that persists between scenes
    public string PlayerName { get; set; }
    public int Currency { get; set; }
    public Vector2 SpawnPosition { get; set; }
    public string PreviousScene { get; set; }
    
    public override void _Ready()
    {
        Instance = this;
    }
}

// When changing scenes:
GameState.Instance.SpawnPosition = doorExitPosition;
GameState.Instance.PreviousScene = GetTree().CurrentScene.SceneFilePath;
SceneManager.Instance.ChangeScene("res://scenes/levels/dungeon.tscn");

// In new scene:
public override void _Ready()
{
    GlobalPosition = GameState.Instance.SpawnPosition;
}
```

### Method 2: Scene Parameters

```csharp
public partial class SceneManager : Node
{
    public Dictionary<string, object> SceneParams { get; private set; } = new();
    
    public void ChangeScene(string path, Dictionary<string, object> parameters = null)
    {
        SceneParams = parameters ?? new();
        // ... change scene ...
    }
}

// Usage:
SceneManager.Instance.ChangeScene("res://scenes/battle.tscn", new Dictionary<string, object>
{
    { "enemy_type", "boss" },
    { "difficulty", 3 }
});

// In battle scene:
public override void _Ready()
{
    var enemyType = SceneManager.Instance.SceneParams["enemy_type"] as string;
    var difficulty = (int)SceneManager.Instance.SceneParams["difficulty"];
}
```

---

## 🚪 Door/Portal System

### Door Node

```csharp
public partial class Door : Area2D
{
    [Export] public string TargetScene;
    [Export] public string TargetDoorId;  // Which door to spawn at
    [Export] public string DoorId;         // This door's ID
    
    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }
    
    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        
        GameState.Instance.TargetDoorId = TargetDoorId;
        SceneManager.Instance.ChangeScene(TargetScene);
    }
}
```

### Spawn at Target Door

```csharp
// In your level/player spawner
public override void _Ready()
{
    var player = GetNode<Player>("Player");
    var targetDoorId = GameState.Instance.TargetDoorId;
    
    if (!string.IsNullOrEmpty(targetDoorId))
    {
        // Find the door and spawn there
        var doors = GetTree().GetNodesInGroup("doors");
        foreach (var d in doors)
        {
            if (d is Door door && door.DoorId == targetDoorId)
            {
                player.GlobalPosition = door.GlobalPosition;
                break;
            }
        }
    }
}
```

---

## 🏗️ Additive Scene Loading

Load scenes on top of current scene (for UI, sub-areas):

```csharp
// Load additive scene
public Node LoadAdditiveScene(string path, Node parent = null)
{
    var scene = GD.Load<PackedScene>(path);
    var instance = scene.Instantiate();
    (parent ?? GetTree().CurrentScene).AddChild(instance);
    return instance;
}

// Example: Load HUD
var hud = LoadAdditiveScene("res://scenes/ui/hud.tscn");

// Example: Load a room
var room = LoadAdditiveScene("res://scenes/rooms/treasure_room.tscn", _roomsContainer);
```

### Unload

```csharp
public void UnloadScene(Node sceneInstance)
{
    sceneInstance.QueueFree();
}
```

---

## 🎮 Game Flow

```
Main Menu
    ↓ [New Game]
Loading Screen
    ↓
Game World ←───────┐
    │              │
    ↓ [Enter Door] │
Loading Screen     │
    ↓              │
New Level ─────────┘
    │
    ↓ [Pause]
Pause Menu (overlay)
    │
    ↓ [Quit to Menu]
Main Menu
```

### Main Menu

```csharp
public partial class MainMenu : Control
{
    public void OnNewGamePressed()
    {
        SaveManager.CurrentData = new SaveData();  // Fresh save
        SceneManager.Instance.ChangeScene("res://scenes/levels/intro.tscn");
    }
    
    public void OnContinuePressed()
    {
        if (SaveManager.Load())
        {
            SceneManager.Instance.ChangeScene(SaveManager.CurrentData.CurrentLevel);
        }
    }
    
    public void OnSettingsPressed()
    {
        // Show settings overlay
        var settings = GD.Load<PackedScene>("res://scenes/ui/settings.tscn").Instantiate();
        AddChild(settings);
    }
    
    public void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
```

---

## 🔄 Scene Restart

```csharp
// Restart current scene
public void RestartScene()
{
    GetTree().ReloadCurrentScene();
}

// For death/retry
public void OnPlayerDied()
{
    // Show death screen, then restart
    await ToSignal(GetTree().CreateTimer(2.0), "timeout");
    GetTree().ReloadCurrentScene();
}
```

---

## 📋 Scene Management Checklist

- [ ] SceneManager singleton for transitions
- [ ] Loading screen for long loads
- [ ] Background loading for large scenes
- [ ] Data persistence via GameState singleton
- [ ] Door/portal system for level connections
- [ ] Scene parameters for specific spawn points
- [ ] Additive loading for UI/overlays

---

**Next Chapter**: [14 - Optimization](14_OPTIMIZATION.md)

