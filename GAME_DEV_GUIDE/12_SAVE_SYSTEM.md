# Chapter 12: Save System

> **Goal**: Persist player progress between sessions.

---

## 📁 File Paths in Godot

| Path Prefix | Description | Example Location |
|-------------|-------------|------------------|
| `res://` | Project files (read-only in export) | `res://scenes/player.tscn` |
| `user://` | User data (writable) | `%AppData%\Godot\app_userdata\YourGame\` |

**Always use `user://` for saves!**

```csharp
// Get actual path for debugging
string path = ProjectSettings.GlobalizePath("user://save.json");
GD.Print(path);  // Shows full system path
```

---

## 💾 Simple JSON Save

### Save Data Class

```csharp
using System.Text.Json;

public class SaveData
{
    public float PlayerHealth { get; set; } = 100;
    public Vector2 PlayerPosition { get; set; } = Vector2.Zero;
    public int Currency { get; set; } = 0;
    public string CurrentLevel { get; set; } = "res://scenes/levels/level1.tscn";
    public List<string> UnlockedAbilities { get; set; } = new();
    public Dictionary<string, bool> Flags { get; set; } = new();
    
    // For Vector2 serialization (System.Text.Json doesn't handle it natively)
    public float PosX { get => PlayerPosition.X; set => PlayerPosition = new Vector2(value, PlayerPosition.Y); }
    public float PosY { get => PlayerPosition.Y; set => PlayerPosition = new Vector2(PlayerPosition.X, value); }
}
```

### Save Manager

```csharp
public static class SaveManager
{
    private const string SavePath = "user://save.json";
    
    public static SaveData CurrentData { get; private set; } = new();
    
    public static void Save()
    {
        string json = JsonSerializer.Serialize(CurrentData, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(json);
            GD.Print("Game saved!");
        }
        else
        {
            GD.PrintErr($"Failed to save: {FileAccess.GetOpenError()}");
        }
    }
    
    public static bool Load()
    {
        if (!FileAccess.FileExists(SavePath))
        {
            GD.Print("No save file found, using defaults");
            CurrentData = new SaveData();
            return false;
        }
        
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file != null)
        {
            string json = file.GetAsText();
            CurrentData = JsonSerializer.Deserialize<SaveData>(json) ?? new SaveData();
            GD.Print("Game loaded!");
            return true;
        }
        
        GD.PrintErr($"Failed to load: {FileAccess.GetOpenError()}");
        return false;
    }
    
    public static bool SaveExists()
    {
        return FileAccess.FileExists(SavePath);
    }
    
    public static void DeleteSave()
    {
        if (FileAccess.FileExists(SavePath))
        {
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
        }
    }
}
```

---

## 🔄 Saving Game State

### Player Integration

```csharp
public partial class Player : CharacterBody2D
{
    public void SaveState()
    {
        SaveManager.CurrentData.PlayerHealth = _combatStats.CurrentHealth;
        SaveManager.CurrentData.PlayerPosition = GlobalPosition;
    }
    
    public void LoadState()
    {
        _combatStats.SetHealth(SaveManager.CurrentData.PlayerHealth);
        GlobalPosition = SaveManager.CurrentData.PlayerPosition;
    }
}
```

### Saving Everything

```csharp
public void SaveGame()
{
    // Collect data from all saveable objects
    var player = GetTree().GetFirstNodeInGroup("player") as Player;
    player?.SaveState();
    
    // Save inventory
    InventoryManager.Instance.SaveToData(SaveManager.CurrentData);
    
    // Save quest progress
    QuestManager.Instance.SaveToData(SaveManager.CurrentData);
    
    // Write to disk
    SaveManager.Save();
}
```

---

## 🎮 Auto-Save

```csharp
public partial class AutoSaveManager : Node
{
    [Export] public float AutoSaveInterval = 300f;  // 5 minutes
    
    private Timer _timer;
    
    public override void _Ready()
    {
        _timer = new Timer();
        _timer.WaitTime = AutoSaveInterval;
        _timer.Timeout += OnAutoSave;
        _timer.Autostart = true;
        AddChild(_timer);
    }
    
    private void OnAutoSave()
    {
        GD.Print("Auto-saving...");
        GameManager.Instance.SaveGame();
        
        // Show indicator
        ShowAutoSaveIcon();
    }
    
    private async void ShowAutoSaveIcon()
    {
        _saveIcon.Visible = true;
        await ToSignal(GetTree().CreateTimer(2.0), "timeout");
        _saveIcon.Visible = false;
    }
}
```

### Save Points

```csharp
public partial class SavePoint : Area2D
{
    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }
    
    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        
        // Full heal at save point
        var player = body as Player;
        player.Heal(player.MaxHealth);
        
        // Save
        SaveManager.CurrentData.PlayerPosition = GlobalPosition;
        GameManager.Instance.SaveGame();
        
        // Visual feedback
        ShowSaveEffect();
    }
}
```

---

## 🗂️ Multiple Save Slots

```csharp
public static class SaveManager
{
    private static int _currentSlot = 0;
    
    private static string GetSavePath(int slot)
    {
        return $"user://save_{slot}.json";
    }
    
    public static void SetSlot(int slot)
    {
        _currentSlot = slot;
    }
    
    public static void Save()
    {
        string json = JsonSerializer.Serialize(CurrentData);
        using var file = FileAccess.Open(GetSavePath(_currentSlot), FileAccess.ModeFlags.Write);
        file?.StoreString(json);
    }
    
    public static bool Load()
    {
        var path = GetSavePath(_currentSlot);
        if (!FileAccess.FileExists(path)) return false;
        
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null) return false;
        
        CurrentData = JsonSerializer.Deserialize<SaveData>(file.GetAsText());
        return true;
    }
    
    public static SaveData[] GetAllSaves()
    {
        var saves = new SaveData[3];  // 3 slots
        for (int i = 0; i < 3; i++)
        {
            var path = GetSavePath(i);
            if (FileAccess.FileExists(path))
            {
                using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                saves[i] = JsonSerializer.Deserialize<SaveData>(file.GetAsText());
            }
        }
        return saves;
    }
}
```

---

## 📊 Settings (Separate from Saves)

```csharp
public class GameSettings
{
    public float MasterVolume { get; set; } = 1f;
    public float MusicVolume { get; set; } = 0.8f;
    public float SfxVolume { get; set; } = 1f;
    public bool Fullscreen { get; set; } = false;
    public int ResolutionIndex { get; set; } = 0;
    public bool Vsync { get; set; } = true;
    public bool ScreenShake { get; set; } = true;
}

public static class SettingsManager
{
    private const string SettingsPath = "user://settings.json";
    
    public static GameSettings Settings { get; private set; } = new();
    
    public static void Save()
    {
        string json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
        using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Write);
        file?.StoreString(json);
    }
    
    public static void Load()
    {
        if (!FileAccess.FileExists(SettingsPath))
        {
            Settings = new GameSettings();
            return;
        }
        
        using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Read);
        if (file != null)
        {
            Settings = JsonSerializer.Deserialize<GameSettings>(file.GetAsText()) ?? new();
        }
    }
    
    public static void Apply()
    {
        // Audio
        AudioManager.Instance.SetMasterVolume(Settings.MasterVolume);
        AudioManager.Instance.SetMusicVolume(Settings.MusicVolume);
        AudioManager.Instance.SetSfxVolume(Settings.SfxVolume);
        
        // Display
        DisplayServer.WindowSetMode(Settings.Fullscreen 
            ? DisplayServer.WindowMode.Fullscreen 
            : DisplayServer.WindowMode.Windowed);
        
        DisplayServer.WindowSetVsyncMode(Settings.Vsync 
            ? DisplayServer.VSyncMode.Enabled 
            : DisplayServer.VSyncMode.Disabled);
    }
}
```

---

## 🔐 Save Data Validation

```csharp
public static void Load()
{
    // ... load JSON ...
    
    // Validate loaded data
    CurrentData = Validate(CurrentData);
}

private static SaveData Validate(SaveData data)
{
    // Clamp health
    data.PlayerHealth = Mathf.Clamp(data.PlayerHealth, 1, 999);
    
    // Ensure lists aren't null
    data.UnlockedAbilities ??= new List<string>();
    data.Flags ??= new Dictionary<string, bool>();
    
    // Validate level exists
    if (!ResourceLoader.Exists(data.CurrentLevel))
    {
        data.CurrentLevel = "res://scenes/levels/level1.tscn";
    }
    
    return data;
}
```

---

## 🎯 Souls-like Save Pattern

For Souls-like games, save on:
- Resting at bonfire/checkpoint
- Picking up items
- Opening shortcuts
- Defeating bosses

**Don't** save:
- Enemy positions (they respawn)
- Player exact position (use last checkpoint)

```csharp
public class SoulsSaveData
{
    public string LastCheckpointId { get; set; }
    public List<string> CollectedItems { get; set; } = new();
    public List<string> DefeatedBosses { get; set; } = new();
    public List<string> UnlockedShortcuts { get; set; } = new();
    public int Souls { get; set; }
    
    // Player stats (not position!)
    public int Level { get; set; }
    public int Vitality { get; set; }
    public int Strength { get; set; }
    // ... etc
}
```

---

## 📋 Save System Checklist

- [ ] Use `user://` for save files
- [ ] JSON serialization for readability
- [ ] Separate settings from game saves
- [ ] Multiple save slots (if needed)
- [ ] Auto-save with visual indicator
- [ ] Validate loaded data
- [ ] Handle missing save files gracefully

---

**Next Chapter**: [13 - Scene Management](13_SCENE_MANAGEMENT.md)

