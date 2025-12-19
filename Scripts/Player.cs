using Godot;
using System;

public partial class Player : CharacterBody2D
{
	// Optional: assign in the Inspector. If empty, Player will look for a Hud in the "hud" group.
	[Export] public NodePath HudPath;
	private Hud _hud;
	[Signal] public delegate void SkillActivatedEventHandler(int slot, float cooldown);

	[Export] public float MoveSpeed = 60f;
	[Export] public Skill[] Skills = new Skill[2]; // Slot 0 and Slot 1
	private float[] _skillCooldowns;

	[Signal] public delegate void HealthChangedEventHandler(float current, float max);
	[Export] public float MaxHealth = 100f;
	[Export] public float Health = 100f;

	private AnimatedSprite2D _sprite;
	
	// State machine variables
	private enum State { Idle, Dashing, Teleporting, Dead }
	private State _currentState = State.Idle;
	private double _stateTimer;
	
	// Dash/TP specific
	private Vector2 _velocityOverride;
	private bool _tpTriggered;
	private Vector2 _tpTarget;
	private double _tpElapsed;
	private float _tpDelaySeconds;
	private float _tpTotalDurationSeconds;
	private bool _deathQueued;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.AnimationFinished += OnSpriteAnimationFinished;

		// Initialize health
		MaxHealth = Mathf.Max(1f, MaxHealth);
		Health = Mathf.Clamp(Health, 0f, MaxHealth);

		_skillCooldowns = new float[Skills.Length];

		BindHud();

		// Push initial health state to HUD
		EmitSignal(SignalName.HealthChanged, Health, MaxHealth);
	}

	private void BindHud()
	{
		_hud = FindHud();
		if (_hud == null)
		{
			GD.PrintErr("HUD not found (set Player.HudPath or add HUD to group 'hud'). UI won't update.");
			return;
		}

		// Player "manages" the HUD by emitting signals; HUD stays a separate scene/node.
		SkillActivated += _hud.OnPlayerSkillActivated;
		HealthChanged += _hud.OnPlayerHealthChanged;
		GD.Print("HUD connected successfully!");
	}

	private Hud FindHud()
	{
		// 1) Prefer explicit NodePath (if assigned in Inspector)
		if (HudPath != null && !HudPath.IsEmpty)
			return GetNodeOrNull<Hud>(HudPath);

		// 2) Fallback: group lookup (keeps Player independent of scene structure)
		return GetTree()?.GetFirstNodeInGroup("hud") as Hud;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_currentState == State.Dead)
		{
			Velocity = Vector2.Zero;
			return;
		}

		// 1. Update cooldown timers (stored on the player, not on the Skill resource)
		for (int i = 0; i < _skillCooldowns.Length; i++)
		{
			_skillCooldowns[i] = Mathf.Max(0, _skillCooldowns[i] - (float)delta);
		}

		// 2. Handle Logic based on State
		switch (_currentState)
		{
			case State.Idle:
				HandleMovement();
				HandleInput();
				break;
			case State.Dashing:
				_stateTimer -= delta;
				Velocity = _velocityOverride;
				MoveAndSlide();
				if (_stateTimer <= 0) _currentState = State.Idle;
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
				// End the teleport state after the configured total duration
				if (_tpElapsed >= _tpTotalDurationSeconds) 
				{
					_currentState = State.Idle;
				}
				break;
		}
	}

	private void HandleMovement()
	{
		Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").Normalized();
		Velocity = input * MoveSpeed;
		
		if (input != Vector2.Zero) PlayAnimation(input);
		else _sprite.Play("idle");

		MoveAndSlide();
	}

	private void HandleInput()
	{
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

	// Capability Methods
	public void ApplyDash(Vector2 dir, float speed, float time)
	{
		if (_currentState == State.Dead) return;
		_currentState = State.Dashing;
		_velocityOverride = dir * speed;
		_stateTimer = time;
	}

	public void ApplyTeleport(Vector2 dir, float dist, float delaySeconds, float totalDurationSeconds)
	{
		if (_currentState == State.Dead) return;
		_currentState = State.Teleporting;
		_stateTimer = totalDurationSeconds; // kept for your debug prints / existing pattern
		_tpTriggered = true;
		_tpElapsed = 0;
		_tpDelaySeconds = delaySeconds;
		_tpTotalDurationSeconds = Mathf.Max(delaySeconds, totalDurationSeconds);
		_tpTarget = GlobalPosition + (dir * dist);
		_sprite.Play("teleport");
	}

	public void Damage(float amount)
	{
		if (amount <= 0) return;
		if (_currentState == State.Dead) return;
		Health = Mathf.Clamp(Health - amount, 0f, MaxHealth);
		EmitSignal(SignalName.HealthChanged, Health, MaxHealth);
		if (Health <= 0) Die();
	}

	public void Heal(float amount)
	{
		if (amount <= 0) return;
		if (_currentState == State.Dead) return;
		Health = Mathf.Clamp(Health + amount, 0f, MaxHealth);
		EmitSignal(SignalName.HealthChanged, Health, MaxHealth);
	}

	private void Die()
	{
		if (_deathQueued) return;
		_deathQueued = true;

		_currentState = State.Dead;
		Velocity = Vector2.Zero;

		// Keep the camera in the world when the player is deleted (camera is a child of Player in player.tscn).
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

		// Disable collisions so the corpse doesn't block / get hit again
		var col = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (col != null) col.Disabled = true;

		// Play death animation if it exists; otherwise just remove immediately
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
		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y)) _sprite.Play(dir.X > 0 ? "walk_r" : "walk_l");
		else _sprite.Play("walk_u_d");
	}
}
