using Godot;

namespace Combat;

/// <summary>
/// Component that provides visual and audio feedback when taking damage.
/// Attach as a child of an entity with CombatStats.
/// </summary>
public partial class HitFeedback : Node
{
	#region Flash Effect
	[ExportGroup("Flash Effect")]
	[Export] public bool EnableFlash { get; set; } = false;
	[Export] public Color FlashColor { get; set; } = new Color(1, 1, 1, 0.8f);
	[Export] public float FlashDuration { get; set; } = 0.1f;
	[Export] public NodePath SpritePath { get; set; }
	#endregion

	#region Hitstop (Freeze Frame)
	[ExportGroup("Hitstop")]
	[Export] public bool EnableHitstop { get; set; } = false;
	[Export] public float HitstopDuration { get; set; } = 0.05f;
	#endregion

	#region Screen Shake
	[ExportGroup("Screen Shake")]
	[Export] public bool EnableScreenShake { get; set; } = false;
	[Export] public float ShakeIntensity { get; set; } = 3f;
	[Export] public float ShakeDuration { get; set; } = 0.1f;
	#endregion

	private CombatStats _stats;
	private CanvasItem _sprite;
	private float _flashTimer;
	private Camera2D _camera;
	private Vector2 _cameraOriginalOffset;
	private float _shakeTimer;
	private float _currentShakeIntensity;

	public override void _Ready()
	{
		// Find CombatStats
		_stats = GetParent().GetNodeOrNull<CombatStats>("CombatStats");
		if (_stats == null)
		{
			_stats = GetNodeOrNull<CombatStats>("../CombatStats");
		}
		
		if (_stats != null)
		{
			// Subscribe to the C# event
			_stats.DamageTaken += OnDamageTaken;
		}
		
		// Find sprite for flash effect
		if (SpritePath != null && !SpritePath.IsEmpty)
		{
			_sprite = GetNodeOrNull<CanvasItem>(SpritePath);
		}
		else
		{
			// Try common sprite node names
			_sprite = GetParent().GetNodeOrNull<CanvasItem>("AnimatedSprite2D");
			_sprite ??= GetParent().GetNodeOrNull<CanvasItem>("Sprite2D");
		}
		
		// Find camera for screen shake
		_camera = GetViewport()?.GetCamera2D();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		
		// Handle flash timer
		if (_flashTimer > 0)
		{
			_flashTimer -= dt;
			if (_flashTimer <= 0 && _sprite != null)
			{
				// Reset modulate
				_sprite.Modulate = Colors.White;
			}
		}
		
		// Handle screen shake
		if (_shakeTimer > 0)
		{
			_shakeTimer -= dt;
			if (_camera != null)
			{
				if (_shakeTimer > 0)
				{
					// Apply random offset
					Vector2 offset = new Vector2(
						(float)GD.RandRange(-_currentShakeIntensity, _currentShakeIntensity),
						(float)GD.RandRange(-_currentShakeIntensity, _currentShakeIntensity)
					);
					_camera.Offset = _cameraOriginalOffset + offset;
				}
				else
				{
					// Reset camera
					_camera.Offset = _cameraOriginalOffset;
				}
			}
		}
	}

	private void OnDamageTaken(DamageInfo info)
	{
		if (EnableFlash) DoFlash();
		if (EnableHitstop) DoHitstop();
		if (EnableScreenShake) DoScreenShake(info.WasCrit ? ShakeIntensity * 2f : ShakeIntensity);
	}

	/// <summary>
	/// Flash the sprite white briefly.
	/// </summary>
	public void DoFlash()
	{
		if (_sprite == null) return;
		
		_sprite.Modulate = FlashColor;
		_flashTimer = FlashDuration;
	}

	/// <summary>
	/// Brief freeze frame effect.
	/// </summary>
	public void DoHitstop()
	{
		// Use a tween to pause and resume
		var tween = GetTree().CreateTween();
		Engine.TimeScale = 0.05f;
		tween.TweenCallback(Callable.From(() => Engine.TimeScale = 1f))
			 .SetDelay(HitstopDuration);
	}

	/// <summary>
	/// Shake the camera.
	/// </summary>
	public void DoScreenShake(float intensity = -1f)
	{
		if (_camera == null)
		{
			_camera = GetViewport()?.GetCamera2D();
		}
		
		if (_camera == null) return;
		
		if (_shakeTimer <= 0)
		{
			_cameraOriginalOffset = _camera.Offset;
		}
		
		_currentShakeIntensity = intensity > 0 ? intensity : ShakeIntensity;
		_shakeTimer = ShakeDuration;
	}
}
