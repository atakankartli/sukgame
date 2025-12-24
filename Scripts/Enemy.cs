using Godot;
using Combat;

public partial class Enemy : CharacterBody2D, IDamageable
{
	#region Exports
	/// <summary>Skill resource to use for attacks (e.g., Fireball.tres).</summary>
	[Export] public Skill AttackSkill;
	
	/// <summary>Fixed direction to fire attacks.</summary>
	[Export] public Vector2 FireDirection = Vector2.Left;
	#endregion

	#region Components
	private AnimatedSprite2D _sprite;
	private CombatStats _combatStats;
	private KnockbackReceiver _knockback;
	#endregion

	#region State
	private float _attackCooldown;
	private bool _isDead;
	#endregion

	#region IDamageable Implementation
	public bool IsAlive => _combatStats?.IsAlive ?? !_isDead;
	public float CurrentHealth => _combatStats?.CurrentHealth ?? 0f;
	public float MaxHealth => _combatStats?.MaxHealth ?? 100f;

	public float TakeDamage(DamageInfo damageInfo)
	{
		if (_combatStats == null) return 0f;
		return _combatStats.ProcessDamage(ref damageInfo);
	}
	#endregion

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("idle");
		_sprite.AnimationFinished += OnAnimationFinished;

		// Find CombatStats component
		_combatStats = GetNodeOrNull<CombatStats>("CombatStats");
		if (_combatStats == null)
		{
			GD.PrintErr($"Enemy '{Name}': CombatStats component not found! Add a CombatStats child node.");
		}
		else
		{
			_combatStats.Died += OnDied;
			_combatStats.PoiseBroken += OnPoiseBroken;
			// Subscribe to the C# event for damage taken
			_combatStats.DamageTaken += OnDamageTaken;
		}

		// Find knockback receiver
		_knockback = GetNodeOrNull<KnockbackReceiver>("KnockbackReceiver");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead) return;
		if (_combatStats != null && !_combatStats.IsAlive) return;

		// Don't attack while being knocked back
		if (_knockback != null && _knockback.IsKnockedBack) return;

		if (AttackSkill == null) return;

		_attackCooldown = Mathf.Max(0, _attackCooldown - (float)delta);
		if (_attackCooldown > 0) return;

		var dir = FireDirection.Normalized();
		if (dir == Vector2.Zero) return;

		if (AttackSkill.Execute(this, dir))
		{
			_attackCooldown = AttackSkill.Cooldown;
		}
	}

	#region Legacy Damage Method (For backwards compatibility)
	/// <summary>
	/// Legacy damage method. Prefer using IDamageable.TakeDamage with DamageInfo.
	/// </summary>
	public void Damage(float amount)
	{
		if (amount <= 0) return;
		if (_isDead) return;

		var info = DamageInfo.Physical(amount, null, Vector2.Zero, 0f);
		TakeDamage(info);
	}
	#endregion

	#region Event Handlers
	private void OnDamageTaken(DamageInfo info)
	{
		// Play hit reaction animation if it exists
		if (_sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation("hit"))
		{
			_sprite.Play("hit");
		}
	}

	private void OnPoiseBroken()
	{
		// Play stagger animation if it exists
		if (_sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation("stagger"))
		{
			_sprite.Play("stagger");
		}
	}

	private void OnDied()
	{
		Die();
	}
	#endregion

	private void Die()
	{
		if (_isDead) return;
		_isDead = true;

		// Disable collision
		var col = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (col != null) col.Disabled = true;

		// Play death animation if it exists
		if (_sprite != null && _sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation("die"))
		{
			_sprite.Play("die");
		}
		else
		{
			QueueFree();
		}
	}

	private void OnAnimationFinished()
	{
		if (_isDead && _sprite.Animation == "die")
		{
			QueueFree();
		}
		else if (_sprite.Animation == "hit" || _sprite.Animation == "stagger")
		{
			_sprite.Play("idle");
		}
	}
}
