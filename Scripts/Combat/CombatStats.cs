using Godot;
using System;

namespace Combat;

/// <summary>
/// Component that holds combat-related stats for any entity.
/// Attach as a child node to Player, Enemy, etc.
/// Handles health, defense, poise, and damage processing.
/// </summary>
public partial class CombatStats : Node
{
	#region Signals (Godot-compatible simple types)
	[Signal] public delegate void HealthChangedEventHandler(float current, float max);
	[Signal] public delegate void HealedEventHandler(float amount);
	[Signal] public delegate void DiedEventHandler();
	[Signal] public delegate void PoiseChangedEventHandler(float current, float max);
	[Signal] public delegate void PoiseBrokenEventHandler();
	#endregion

	#region C# Events (for complex types like DamageInfo)
	/// <summary>C# event fired when damage is taken. Use this for complex damage info.</summary>
	public event Action<DamageInfo> DamageTaken;
	#endregion

	#region Health Stats
	[ExportGroup("Health")]
	[Export] public float MaxHealth { get; set; } = 100f;
	public float CurrentHealth { get; private set; }
	public bool IsAlive => CurrentHealth > 0;
	#endregion

	#region Defense Stats
	[ExportGroup("Defense")]
	/// <summary>Flat damage reduction applied before percentage.</summary>
	[Export] public float Defense { get; set; } = 0f;
	
	/// <summary>Percentage damage reduction (0.0 to 1.0). Applied after flat defense.</summary>
	[Export(PropertyHint.Range, "0,1,0.01")] public float DamageReduction { get; set; } = 0f;
	
	/// <summary>Resistances per damage type. Negative values = weakness.</summary>
	[Export] public Godot.Collections.Dictionary<DamageType, float> Resistances { get; set; } = new();
	#endregion

	#region Poise (Stagger System)
	[ExportGroup("Poise")]
	/// <summary>Maximum poise. Higher = harder to stagger.</summary>
	[Export] public float MaxPoise { get; set; } = 50f;
	
	/// <summary>How fast poise regenerates per second when not taking hits.</summary>
	[Export] public float PoiseRegenRate { get; set; } = 20f;
	
	/// <summary>Delay before poise starts regenerating after taking a hit.</summary>
	[Export] public float PoiseRegenDelay { get; set; } = 2f;
	
	public float CurrentPoise { get; private set; }
	private float _poiseRegenTimer;
	#endregion

	#region Critical Hits
	[ExportGroup("Critical Hits")]
	[Export(PropertyHint.Range, "0,1,0.01")] public float CritChance { get; set; } = 0.05f;
	[Export] public float CritMultiplier { get; set; } = 1.5f;
	#endregion

	#region Invincibility
	[ExportGroup("Invincibility")]
	/// <summary>Duration of i-frames after taking damage.</summary>
	[Export] public float InvincibilityDuration { get; set; } = 0f;
	
	public bool IsInvincible { get; private set; }
	private float _invincibilityTimer;
	#endregion

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
		CurrentPoise = MaxPoise;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		
		// Handle invincibility timer
		if (_invincibilityTimer > 0)
		{
			_invincibilityTimer -= dt;
			if (_invincibilityTimer <= 0)
				IsInvincible = false;
		}
		
		// Handle poise regeneration
		if (_poiseRegenTimer > 0)
		{
			_poiseRegenTimer -= dt;
		}
		else if (CurrentPoise < MaxPoise)
		{
			CurrentPoise = Mathf.Min(MaxPoise, CurrentPoise + PoiseRegenRate * dt);
		}
	}

	/// <summary>
	/// Process incoming damage through the defense pipeline.
	/// Returns the final damage dealt.
	/// </summary>
	public float ProcessDamage(ref DamageInfo info)
	{
		if (!IsAlive) return 0f;
		if (IsInvincible) return 0f;

		float damage = info.BaseDamage;
		
		// 1. Apply resistance/weakness
		if (Resistances.TryGetValue(info.Type, out float resistance))
		{
			damage *= (1f - resistance);
		}
		
		// 2. Apply flat defense (minimum 1 damage unless fully resisted)
		damage = Mathf.Max(0, damage - Defense);
		
		// 3. Apply percentage reduction
		damage *= (1f - DamageReduction);
		
		// 4. Check for critical hit
		if (info.CanCrit && GD.Randf() < CritChance)
		{
			damage *= CritMultiplier;
			info.WasCrit = true;
		}
		
		// 5. Apply poise damage
		if (info.PoiseDamage > 0)
		{
			CurrentPoise -= info.PoiseDamage;
			_poiseRegenTimer = PoiseRegenDelay;
			EmitSignal(SignalName.PoiseChanged, CurrentPoise, MaxPoise);
			
			if (CurrentPoise <= 0)
			{
				CurrentPoise = 0;
				EmitSignal(SignalName.PoiseBroken);
			}
		}
		
		// 6. Apply health damage
		info.FinalDamage = damage;
		CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
		
		// 7. Start invincibility frames
		if (InvincibilityDuration > 0)
		{
			IsInvincible = true;
			_invincibilityTimer = InvincibilityDuration;
		}
		
		// 8. Fire events and signals
		DamageTaken?.Invoke(info);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
		
		if (!IsAlive)
		{
			EmitSignal(SignalName.Died);
		}
		
		return damage;
	}

	/// <summary>
	/// Heal the entity.
	/// </summary>
	public void Heal(float amount)
	{
		if (!IsAlive || amount <= 0) return;
		
		float oldHealth = CurrentHealth;
		CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
		float actualHeal = CurrentHealth - oldHealth;
		
		if (actualHeal > 0)
		{
			EmitSignal(SignalName.Healed, actualHeal);
			EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
		}
	}

	/// <summary>
	/// Reset poise to maximum (e.g., after stagger recovery).
	/// </summary>
	public void ResetPoise()
	{
		CurrentPoise = MaxPoise;
		EmitSignal(SignalName.PoiseChanged, CurrentPoise, MaxPoise);
	}

	/// <summary>
	/// Force invincibility for a duration (e.g., during dodge).
	/// </summary>
	public void SetInvincible(float duration)
	{
		IsInvincible = true;
		_invincibilityTimer = Mathf.Max(_invincibilityTimer, duration);
	}

	/// <summary>
	/// Immediately end invincibility.
	/// </summary>
	public void ClearInvincibility()
	{
		IsInvincible = false;
		_invincibilityTimer = 0;
	}
}
