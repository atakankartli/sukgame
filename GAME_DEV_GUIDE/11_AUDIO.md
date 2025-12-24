# Chapter 11: Audio

> **Goal**: Add immersive sound effects and music to your game.

---

## 🔊 Audio Nodes

| Node | Use For |
|------|---------|
| `AudioStreamPlayer` | Global audio (music, UI sounds) |
| `AudioStreamPlayer2D` | Positional audio (enemies, pickups) |
| `AudioStreamPlayer3D` | 3D positional audio |

---

## 🎵 Basic Audio Playback

### One-Shot Sound

```csharp
public void PlaySound(AudioStream sound)
{
    var player = new AudioStreamPlayer();
    AddChild(player);
    player.Stream = sound;
    player.Play();
    
    // Auto-cleanup when done
    player.Finished += () => player.QueueFree();
}
```

### Reusable AudioStreamPlayer

```csharp
public partial class Enemy : CharacterBody2D
{
    [Export] public AudioStream HitSound;
    [Export] public AudioStream DeathSound;
    
    private AudioStreamPlayer2D _audioPlayer;
    
    public override void _Ready()
    {
        _audioPlayer = GetNode<AudioStreamPlayer2D>("AudioStreamPlayer2D");
    }
    
    public void OnHit()
    {
        _audioPlayer.Stream = HitSound;
        _audioPlayer.PitchScale = (float)GD.RandRange(0.9, 1.1);  // Variation
        _audioPlayer.Play();
    }
}
```

---

## 🎹 Audio Manager (Singleton)

```csharp
// scripts/Core/AudioManager.cs
public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; }
    
    private AudioStreamPlayer _musicPlayer;
    private List<AudioStreamPlayer> _sfxPool = new();
    private const int PoolSize = 16;
    
    [Export] public float MasterVolume = 1f;
    [Export] public float MusicVolume = 0.8f;
    [Export] public float SfxVolume = 1f;
    
    public override void _Ready()
    {
        Instance = this;
        
        // Music player
        _musicPlayer = new AudioStreamPlayer();
        _musicPlayer.Bus = "Music";
        AddChild(_musicPlayer);
        
        // SFX pool
        for (int i = 0; i < PoolSize; i++)
        {
            var player = new AudioStreamPlayer();
            player.Bus = "SFX";
            AddChild(player);
            _sfxPool.Add(player);
        }
    }
    
    public void PlayMusic(AudioStream music, float fadeTime = 1f)
    {
        if (_musicPlayer.Stream == music) return;
        
        // Fade out current
        var tween = CreateTween();
        tween.TweenProperty(_musicPlayer, "volume_db", -80f, fadeTime);
        tween.TweenCallback(Callable.From(() => {
            _musicPlayer.Stream = music;
            _musicPlayer.VolumeDb = 0;
            _musicPlayer.Play();
        }));
    }
    
    public void PlaySFX(AudioStream sound, float pitchVariation = 0f)
    {
        // Find available player
        var player = _sfxPool.FirstOrDefault(p => !p.Playing);
        if (player == null) return;
        
        player.Stream = sound;
        player.PitchScale = 1f + (float)GD.RandRange(-pitchVariation, pitchVariation);
        player.Play();
    }
    
    public void StopMusic(float fadeTime = 1f)
    {
        var tween = CreateTween();
        tween.TweenProperty(_musicPlayer, "volume_db", -80f, fadeTime);
        tween.TweenCallback(Callable.From(() => _musicPlayer.Stop()));
    }
}
```

**Usage:**
```csharp
AudioManager.Instance.PlaySFX(hitSound, 0.1f);
AudioManager.Instance.PlayMusic(battleMusic);
```

---

## 🔉 Audio Buses

Configure in: **Project → Audio → Audio Bus Layout**

### Recommended Setup

```
Master (default)
├── Music       (background music)
├── SFX         (sound effects)
│   ├── Combat  (attacks, hits)
│   └── UI      (menu sounds)
└── Ambient     (environment sounds)
```

### Setting Bus

```csharp
_audioPlayer.Bus = "SFX";

// Or by index
_audioPlayer.Bus = AudioServer.GetBusName(1);
```

### Volume Control

```csharp
public void SetMusicVolume(float linear)  // 0.0 to 1.0
{
    int busIdx = AudioServer.GetBusIndex("Music");
    float db = Mathf.LinearToDb(linear);
    AudioServer.SetBusVolumeDb(busIdx, db);
}

public void MuteMusic(bool mute)
{
    int busIdx = AudioServer.GetBusIndex("Music");
    AudioServer.SetBusMute(busIdx, mute);
}
```

---

## 🎭 Positional Audio (2D)

```csharp
public partial class Enemy : CharacterBody2D
{
    private AudioStreamPlayer2D _audio;
    
    public override void _Ready()
    {
        _audio = new AudioStreamPlayer2D();
        _audio.MaxDistance = 500f;      // Falloff distance
        _audio.Attenuation = 1f;        // How quickly it fades
        _audio.Bus = "SFX";
        AddChild(_audio);
    }
}
```

**Properties:**
- `MaxDistance` - Sound silent beyond this
- `Attenuation` - Falloff curve (higher = faster fade)
- `PanningStrength` - Stereo panning (1 = full stereo)

---

## 🎲 Sound Variation

### Random Pitch

```csharp
public void PlayWithVariation(AudioStream sound)
{
    _player.Stream = sound;
    _player.PitchScale = (float)GD.RandRange(0.9, 1.1);
    _player.Play();
}
```

### Random Sound from Array

```csharp
[Export] public AudioStream[] FootstepSounds;

public void PlayFootstep()
{
    if (FootstepSounds.Length == 0) return;
    
    var sound = FootstepSounds[GD.RandRange(0, FootstepSounds.Length - 1)];
    _player.Stream = sound;
    _player.PitchScale = (float)GD.RandRange(0.95, 1.05);
    _player.Play();
}
```

---

## 🎼 Music Transitions

### Crossfade

```csharp
public void CrossfadeMusic(AudioStream newMusic, float duration = 2f)
{
    // Create temporary player for new music
    var newPlayer = new AudioStreamPlayer();
    newPlayer.Stream = newMusic;
    newPlayer.Bus = "Music";
    newPlayer.VolumeDb = -80f;
    AddChild(newPlayer);
    newPlayer.Play();
    
    var tween = CreateTween();
    tween.SetParallel(true);
    
    // Fade out old
    tween.TweenProperty(_musicPlayer, "volume_db", -80f, duration);
    
    // Fade in new
    tween.TweenProperty(newPlayer, "volume_db", 0f, duration);
    
    tween.SetParallel(false);
    tween.TweenCallback(Callable.From(() => {
        _musicPlayer.Stop();
        _musicPlayer.Stream = newMusic;
        _musicPlayer.VolumeDb = 0;
        _musicPlayer.Play();
        newPlayer.QueueFree();
    }));
}
```

---

## 📝 Sound Design Tips

### 1. Layer Sounds
```csharp
// Sword swing = whoosh + impact + voice grunt
public void PlaySwordHit()
{
    PlaySFX(whooshSound);
    PlaySFX(metalImpactSound);
    if (GD.Randf() < 0.3f)  // Sometimes
        PlaySFX(gruntSound);
}
```

### 2. Limiting Sounds

```csharp
// Prevent sound spam
private float _lastFootstepTime;
private const float FootstepInterval = 0.25f;

public void TryPlayFootstep()
{
    if (Time.GetTicksMsec() - _lastFootstepTime < FootstepInterval * 1000)
        return;
    
    PlayFootstep();
    _lastFootstepTime = Time.GetTicksMsec();
}
```

### 3. Contextual Music

```csharp
public void UpdateMusic()
{
    var enemies = GetTree().GetNodesInGroup("enemies");
    bool inCombat = enemies.Any(e => (e as Enemy).IsAggroed);
    
    if (inCombat && _currentMusic != _combatMusic)
        CrossfadeMusic(_combatMusic);
    else if (!inCombat && _currentMusic != _explorationMusic)
        CrossfadeMusic(_explorationMusic);
}
```

---

## 📋 Audio Checklist

- [ ] Audio buses configured (Music, SFX, etc.)
- [ ] AudioManager singleton for global access
- [ ] SFX pool to prevent creating nodes per sound
- [ ] Positional audio for enemies/pickups
- [ ] Pitch variation for repeated sounds
- [ ] Music crossfading
- [ ] Volume controls in settings

---

**Next Chapter**: [12 - Save System](12_SAVE_SYSTEM.md)

