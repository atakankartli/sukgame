using Godot;

namespace Combat;

/// <summary>
/// A world-space health bar that displays above any entity with CombatStats.
/// Add as a child of any entity (Player, Enemy, destructible objects, etc.)
/// </summary>
public partial class HealthBar : Node2D
{
	#region Configuration
	[ExportGroup("Appearance")]
	[Export] public Vector2 Size { get; set; } = new Vector2(32, 4);
	[Export] public Vector2 Offset { get; set; } = new Vector2(0, -20);
	[Export] public Color BackgroundColor { get; set; } = new Color(0.2f, 0.2f, 0.2f, 0.8f);
	[Export] public Color HealthColor { get; set; } = new Color(0.2f, 0.8f, 0.2f, 1.0f);
	[Export] public Color DamageColor { get; set; } = new Color(0.9f, 0.2f, 0.2f, 1.0f);
	[Export] public Color BorderColor { get; set; } = new Color(0.1f, 0.1f, 0.1f, 1.0f);
	[Export] public float BorderWidth { get; set; } = 1f;
	
	[ExportGroup("Behavior")]
	/// <summary>If true, bar is hidden when health is full.</summary>
	[Export] public bool HideWhenFull { get; set; } = true;
	
	/// <summary>If true, bar fades out after not taking damage for a while.</summary>
	[Export] public bool AutoHide { get; set; } = true;
	
	/// <summary>Seconds before auto-hiding.</summary>
	[Export] public float AutoHideDelay { get; set; } = 3.0f;
	
	/// <summary>How fast the "damage" bar catches up to actual health.</summary>
	[Export] public float DamageBarSpeed { get; set; } = 2.0f;
	
	[ExportGroup("Animation")]
	[Export] public float FadeSpeed { get; set; } = 3.0f;
	#endregion

	private CombatStats _stats;
	private float _displayedHealth;
	private float _damageBarHealth; // Trails behind for "damage taken" effect
	private float _autoHideTimer;
	private float _targetAlpha;
	private float _currentAlpha;
	
	// Visual elements
	private ColorRect _background;
	private ColorRect _damageBar;
	private ColorRect _healthBar;
	private ColorRect _border;

	public override void _Ready()
	{
		// Find CombatStats in parent hierarchy
		_stats = FindCombatStats();
		if (_stats == null)
		{
			GD.PrintErr($"HealthBar '{Name}': Could not find CombatStats!");
			return;
		}
		
		// Initialize values
		_displayedHealth = _stats.CurrentHealth;
		_damageBarHealth = _stats.CurrentHealth;
		
		// Create visual elements
		CreateHealthBarVisuals();
		
		// Connect to events
		_stats.HealthChanged += OnHealthChanged;
		_stats.DamageTaken += OnDamageTaken;
		
		// Initial visibility
		if (HideWhenFull && _stats.CurrentHealth >= _stats.MaxHealth)
		{
			_targetAlpha = 0f;
			_currentAlpha = 0f;
		}
		else
		{
			_targetAlpha = 1f;
			_currentAlpha = 1f;
		}
		
		UpdateVisuals();
	}

	private CombatStats FindCombatStats()
	{
		// Check parent for CombatStats child
		var parent = GetParent();
		if (parent == null) return null;
		
		var stats = parent.GetNodeOrNull<CombatStats>("CombatStats");
		if (stats != null) return stats;
		
		// Check siblings
		foreach (var child in parent.GetChildren())
		{
			if (child is CombatStats cs) return cs;
		}
		
		return null;
	}

	private void CreateHealthBarVisuals()
	{
		Position = Offset;
		
		// Background (dark)
		_background = new ColorRect();
		_background.Size = Size;
		_background.Position = -Size / 2;
		_background.Color = BackgroundColor;
		AddChild(_background);
		
		// Damage bar (red, shows damage taken)
		_damageBar = new ColorRect();
		_damageBar.Size = Size;
		_damageBar.Position = -Size / 2;
		_damageBar.Color = DamageColor;
		AddChild(_damageBar);
		
		// Health bar (green, actual health)
		_healthBar = new ColorRect();
		_healthBar.Size = Size;
		_healthBar.Position = -Size / 2;
		_healthBar.Color = HealthColor;
		AddChild(_healthBar);
		
		// Border (optional outline)
		if (BorderWidth > 0)
		{
			// We'll draw the border in _Draw instead for cleaner look
		}
	}

	public override void _Process(double delta)
	{
		if (_stats == null) return;
		
		float dt = (float)delta;
		
		// Smoothly update displayed health
		_displayedHealth = _stats.CurrentHealth;
		
		// Damage bar trails behind
		if (_damageBarHealth > _displayedHealth)
		{
			_damageBarHealth = Mathf.MoveToward(_damageBarHealth, _displayedHealth, 
				_stats.MaxHealth * DamageBarSpeed * dt);
		}
		else
		{
			_damageBarHealth = _displayedHealth;
		}
		
		// Auto-hide timer
		if (AutoHide && _targetAlpha > 0)
		{
			_autoHideTimer -= dt;
			if (_autoHideTimer <= 0)
			{
				_targetAlpha = 0f;
			}
		}
		
		// Fade animation
		if (_currentAlpha != _targetAlpha)
		{
			_currentAlpha = Mathf.MoveToward(_currentAlpha, _targetAlpha, FadeSpeed * dt);
		}
		
		UpdateVisuals();
	}

	private void OnHealthChanged(float current, float max)
	{
		// Show the bar
		_targetAlpha = 1f;
		_autoHideTimer = AutoHideDelay;
		
		// If health is full and we should hide when full
		if (HideWhenFull && current >= max)
		{
			_autoHideTimer = 0.5f; // Short delay before hiding
		}
	}

	private void OnDamageTaken(DamageInfo info)
	{
		// Show bar immediately when taking damage
		_targetAlpha = 1f;
		_autoHideTimer = AutoHideDelay;
	}

	private void UpdateVisuals()
	{
		if (_stats == null) return;
		
		float healthPercent = _stats.MaxHealth > 0 ? _displayedHealth / _stats.MaxHealth : 0;
		float damagePercent = _stats.MaxHealth > 0 ? _damageBarHealth / _stats.MaxHealth : 0;
		
		// Update bar widths
		_healthBar.Size = new Vector2(Size.X * healthPercent, Size.Y);
		_damageBar.Size = new Vector2(Size.X * damagePercent, Size.Y);
		
		// Update alpha
		Modulate = new Color(1, 1, 1, _currentAlpha);
	}

	public override void _Draw()
	{
		if (BorderWidth <= 0) return;
		
		// Draw border around the health bar
		var rect = new Rect2(-Size / 2 - new Vector2(BorderWidth, BorderWidth), 
							Size + new Vector2(BorderWidth * 2, BorderWidth * 2));
		DrawRect(rect, BorderColor, false, BorderWidth);
	}

	/// <summary>
	/// Force the health bar to show.
	/// </summary>
	public void Show()
	{
		_targetAlpha = 1f;
		_autoHideTimer = AutoHideDelay;
	}

	/// <summary>
	/// Force the health bar to hide.
	/// </summary>
	public void Hide()
	{
		_targetAlpha = 0f;
	}
}
