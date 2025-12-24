using Godot;
using Combat;
using Combat.Weapons;

public partial class Player : CharacterBody2D, IDamageable
{
	#region Exports & References
	[Export] public NodePath HudPath;
	[Export] public float MoveSpeed = 60f;
	[Export] public Skill[] Skills = new Skill[2];
	
	/// <summary>I-frame duration during dash (Souls-like dodge).</summary>
	[Export] public float DashInvincibilityDuration = 0.15f;
	#endregion

	#region Components
	private Hud _hud;
	private AnimatedSprite2D _sprite;
	private CombatStats _combatStats;
	private KnockbackReceiver _knockback;
	private MeleeWeapon _meleeWeapon;
	#endregion

	#region Signals
	[Signal] public delegate void SkillActivatedEventHandler(int slot, float cooldown);
	[Signal] public delegate void HealthChangedEventHandler(float current, float max);
	#endregion

	#region State Machine
	private enum State { Idle, Attacking, Dashing, Teleporting, Staggered, Dead }
	private State _currentState = State.Idle;
	private double _stateTimer;
	#endregion

	#region Facing Direction
	/// <summary>Last direction the player was facing (for attacks when stationary).</summary>
	private Vector2 _facingDirection = Vector2.Right;
	private AttackDirection _lastAttackDirection = AttackDirection.Right;
	#endregion

	#region Dash/Teleport State
	private Vector2 _velocityOverride;
	private bool _tpTriggered;
	private Vector2 _tpTarget;
	private double _tpElapsed;
	private float _tpDelaySeconds;
	private float _tpTotalDurationSeconds;
	private bool _deathQueued;
	private float[] _skillCooldowns;
	#endregion

	#region IDamageable Implementation
	public bool IsAlive => _combatStats?.IsAlive ?? false;
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
		_sprite.AnimationFinished += OnSpriteAnimationFinished;

		// Find or create CombatStats component
		_combatStats = GetNodeOrNull<CombatStats>("CombatStats");
		if (_combatStats == null)
		{
			GD.PrintErr("Player: CombatStats component not found! Add a CombatStats child node.");
		}
		else
		{
			// Connect to combat events
			_combatStats.HealthChanged += OnHealthChanged;
			_combatStats.Died += OnDied;
			_combatStats.PoiseBroken += OnPoiseBroken;
		}

		// Find knockback receiver
		_knockback = GetNodeOrNull<KnockbackReceiver>("KnockbackReceiver");

		// Find melee weapon component
		_meleeWeapon = GetNodeOrNull<MeleeWeapon>("MeleeWeapon");
		if (_meleeWeapon != null)
		{
			_meleeWeapon.AttackEnded += OnAttackEnded;
		}

		_skillCooldowns = new float[Skills.Length];

		BindHud();

		// Push initial health state to HUD
		if (_combatStats != null)
		{
			EmitSignal(SignalName.HealthChanged, _combatStats.CurrentHealth, _combatStats.MaxHealth);
		}
	}

	private void BindHud()
	{
		_hud = FindHud();
		if (_hud == null)
		{
			GD.PrintErr("HUD not found (set Player.HudPath or add HUD to group 'hud'). UI won't update.");
			return;
		}

		SkillActivated += _hud.OnPlayerSkillActivated;
		HealthChanged += _hud.OnPlayerHealthChanged;
		GD.Print("HUD connected successfully!");
	}

	private Hud FindHud()
	{
		if (HudPath != null && !HudPath.IsEmpty)
			return GetNodeOrNull<Hud>(HudPath);
		return GetTree()?.GetFirstNodeInGroup("hud") as Hud;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_currentState == State.Dead)
		{
			Velocity = Vector2.Zero;
			return;
		}

		// Update skill cooldowns
		for (int i = 0; i < _skillCooldowns.Length; i++)
		{
			_skillCooldowns[i] = Mathf.Max(0, _skillCooldowns[i] - (float)delta);
		}

		// Handle knockback (takes priority over normal movement)
		if (_knockback != null && _knockback.IsKnockedBack)
		{
			// Knockback is handled by KnockbackReceiver component
			return;
		}

		switch (_currentState)
		{
			case State.Idle:
				HandleMovement();
				HandleInput();
				break;
				
			case State.Attacking:
				// Can't move during attack (Souls-like commitment)
				Velocity = Vector2.Zero;
				// MeleeWeapon handles timing, we just wait for AttackEnded signal
				break;
				
			case State.Dashing:
				_stateTimer -= delta;
				Velocity = _velocityOverride;
				MoveAndSlide();
				if (_stateTimer <= 0)
				{
					_currentState = State.Idle;
					// End invincibility slightly after dash ends for better feel
					_combatStats?.ClearInvincibility();
				}
				break;
				
			case State.Teleporting:
				_stateTimer -= delta;
				Velocity = Vector2.Zero;
				_tpElapsed += delta;
				if (_tpTriggered && _tpElapsed >= _tpDelaySeconds)
				{
					GlobalPosition = _tpTarget;
					_tpTriggered = false;
				}
				if (_tpElapsed >= _tpTotalDurationSeconds)
				{
					_currentState = State.Idle;
				}
				break;
				
			case State.Staggered:
				_stateTimer -= delta;
				Velocity = Vector2.Zero;
				if (_stateTimer <= 0)
				{
					_currentState = State.Idle;
					_combatStats?.ResetPoise();
				}
				break;
		}
	}

	private void HandleMovement()
	{
		Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").Normalized();
		Velocity = input * MoveSpeed;

		if (input != Vector2.Zero)
		{
			// Update facing direction when moving
			_facingDirection = input;
			_lastAttackDirection = MeleeWeapon.GetDirectionFromVector(input);
			PlayAnimation(input);
		}
		else
		{
			_sprite.Play("idle");
		}

		MoveAndSlide();
	}

	private void HandleInput()
	{
		// --- Melee Attack (Left Mouse Button) ---
		if (Input.IsActionJustPressed("attack"))
		{
			TryMeleeAttack();
		}

		// --- Skills (Keys 1 and 2) ---
		Vector2 moveInput = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").Normalized();
		Vector2 mouseDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();

		for (int i = 0; i < Skills.Length; i++)
		{
			if (Input.IsActionJustPressed($"skill{i + 1}") && Skills[i] != null && _skillCooldowns[i] <= 0)
			{
				var skill = Skills[i];
				Vector2 dir = skill.AimSource == SkillAimSource.Mouse ? mouseDir : moveInput;
				if (dir == Vector2.Zero) continue;

				if (skill.Execute(this, dir))
				{
					_skillCooldowns[i] = skill.Cooldown;
					EmitSignal(SignalName.SkillActivated, i, skill.Cooldown);
				}
			}
		}
	}

	private void TryMeleeAttack()
	{
		if (_meleeWeapon == null) return;
		if (_currentState != State.Idle) return;

		// Determine attack direction: prefer mouse direction, fallback to facing
		Vector2 mouseDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
		AttackDirection attackDir;
		
		if (mouseDir != Vector2.Zero)
		{
			attackDir = MeleeWeapon.GetDirectionFromVector(mouseDir);
		}
		else
		{
			attackDir = _lastAttackDirection;
		}

		// Try to start the attack
		if (_meleeWeapon.Attack(attackDir))
		{
			_currentState = State.Attacking;
			_lastAttackDirection = attackDir;
		}
	}

	private void OnAttackEnded()
	{
		// Return to idle when attack animation/timing completes
		if (_currentState == State.Attacking)
		{
			_currentState = State.Idle;
		}
	}

	#region Capability Methods (Called by Skills)
	public void ApplyDash(Vector2 dir, float speed, float time)
	{
		if (_currentState == State.Dead) return;
		if (_currentState == State.Attacking) return; // Can't dash during attack
		
		_currentState = State.Dashing;
		_velocityOverride = dir * speed;
		_stateTimer = time;
		
		// Grant i-frames during dash (Souls-like dodge)
		_combatStats?.SetInvincible(DashInvincibilityDuration);
	}

	public void ApplyTeleport(Vector2 dir, float dist, float delaySeconds, float totalDurationSeconds)
	{
		if (_currentState == State.Dead) return;
		if (_currentState == State.Attacking) return; // Can't teleport during attack
		
		_currentState = State.Teleporting;
		_stateTimer = totalDurationSeconds;
		_tpTriggered = true;
		_tpElapsed = 0;
		_tpDelaySeconds = delaySeconds;
		_tpTotalDurationSeconds = Mathf.Max(delaySeconds, totalDurationSeconds);
		_tpTarget = GlobalPosition + (dir * dist);
		_sprite.Play("teleport");
	}
	#endregion

	#region Legacy Damage Methods (For backwards compatibility with old Fireball)
	/// <summary>
	/// Legacy damage method. Prefer using IDamageable.TakeDamage with DamageInfo.
	/// </summary>
	public void Damage(float amount)
	{
		if (amount <= 0) return;
		if (_currentState == State.Dead) return;
		
		var info = DamageInfo.Physical(amount, null, Vector2.Zero, 0f);
		TakeDamage(info);
	}

	public void Heal(float amount)
	{
		_combatStats?.Heal(amount);
	}
	#endregion

	#region Event Handlers
	private void OnHealthChanged(float current, float max)
	{
		EmitSignal(SignalName.HealthChanged, current, max);
	}

	private void OnDied()
	{
		Die();
	}

	private void OnPoiseBroken()
	{
		// Enter stagger state when poise breaks
		if (_currentState == State.Dead) return;
		_currentState = State.Staggered;
		_stateTimer = 1.0; // 1 second stagger duration
		
		// Play stagger animation if it exists
		if (_sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation("stagger"))
		{
			_sprite.Play("stagger");
		}
	}
	#endregion

	private void Die()
	{
		if (_deathQueued) return;
		_deathQueued = true;

		_currentState = State.Dead;
		Velocity = Vector2.Zero;

		// Keep camera in world when player dies
		var camera = GetNodeOrNull<Camera2D>("Camera2D");
		if (camera != null)
		{
			var sceneRoot = GetTree()?.CurrentScene;
			if (sceneRoot != null && camera.GetParent() == this)
			{
				var camXform = camera.GlobalTransform;
				RemoveChild(camera);
				sceneRoot.AddChild(camera);
				camera.GlobalTransform = camXform;
				camera.MakeCurrent();
			}
		}

		// Disable collisions
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

	private void OnSpriteAnimationFinished()
	{
		if (_currentState != State.Dead) return;
		if (_sprite == null) return;
		if (_sprite.Animation != "die") return;

		QueueFree();
	}

	private void PlayAnimation(Vector2 dir)
	{
		// Don't override attack animations
		if (_currentState == State.Attacking) return;
		
		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y)) _sprite.Play(dir.X > 0 ? "walk_r" : "walk_l");
		else _sprite.Play("walk_u_d");
	}
}
