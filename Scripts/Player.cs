using Godot;
using System;

public partial class Player : CharacterBody2D
{
	// Optional: assign in the Inspector. If empty, Player will auto-find the first Hud in the current scene.
	[Export] public NodePath HudPath;
	private Hud _hud;
	[Signal] public delegate void SkillActivatedEventHandler(int slot, float cooldown);

	[Export] public float MoveSpeed = 60f;
	[Export] public Skill[] Skills = new Skill[2]; // Slot 0 and Slot 1

	[Signal] public delegate void HealthChangedEventHandler(float current, float max);
	[Export] public float MaxHealth = 100f;
	[Export] public float Health = 100f;

	private AnimatedSprite2D _sprite;
	
	// State machine variables
	private enum State { Idle, Dashing, Teleporting }
	private State _currentState = State.Idle;
	private double _stateTimer;
	
	// Dash/TP specific
	private Vector2 _velocityOverride;
	private bool _tpTriggered;
	private Vector2 _tpTarget;
	private double _tpElapsed;
	private float _tpDelaySeconds;
	private float _tpTotalDurationSeconds;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		// Initialize health
		MaxHealth = Mathf.Max(1f, MaxHealth);
		Health = Mathf.Clamp(Health, 0f, MaxHealth);

		// Get reference to HUD (generalized)
		_hud = ResolveHud();
		if (_hud != null)
		{
			SkillActivated += _hud.OnPlayerSkillActivated;
			HealthChanged += _hud.OnPlayerHealthChanged;
			GD.Print("HUD connected successfully!");
		}
		else
		{
			GD.PrintErr("HUD not found. Cooldown UI won't update until a Hud exists in the scene.");
		}

		// Push initial health state to HUD
		EmitSignal(SignalName.HealthChanged, Health, MaxHealth);
	}

	private Hud ResolveHud()
	{
		// 1) Prefer explicit NodePath (if assigned in Inspector)
		if (HudPath != null && !HudPath.IsEmpty)
		{
			var byPath = GetNodeOrNull<Hud>(HudPath);
			if (byPath != null) return byPath;
		}

		// 2) Fallback: search the current scene tree for the first Hud node
		var root = GetTree()?.CurrentScene;
		if (root == null) return null;

		return FindFirstHud(root);
	}

	private static Hud FindFirstHud(Node node)
	{
		if (node is Hud hud) return hud;

		foreach (var child in node.GetChildren())
		{
			if (child is Node childNode)
			{
				var found = FindFirstHud(childNode);
				if (found != null) return found;
			}
		}

		return null;
	}

	public override void _PhysicsProcess(double delta)
	{
		// 1. Update internal skill timers
		foreach (var s in Skills) s?.UpdateTimer(delta);

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
		Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").Normalized();
		for (int i = 0; i < Skills.Length; i++)
		{
			if (Input.IsActionJustPressed($"skill{i + 1}") && Skills[i] != null && Skills[i].IsReady)
			{
				if (Skills[i].Execute(this, input))
					EmitSignal(SignalName.SkillActivated, i, Skills[i].Cooldown);
			}
		}
	}

	// Capability Methods
	public void ApplyDash(Vector2 dir, float speed, float time)
	{
		_currentState = State.Dashing;
		_velocityOverride = dir * speed;
		_stateTimer = time;
	}

	public void ApplyTeleport(Vector2 dir, float dist, float delaySeconds, float totalDurationSeconds)
	{
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
		Health = Mathf.Clamp(Health - amount, 0f, MaxHealth);
		EmitSignal(SignalName.HealthChanged, Health, MaxHealth);
	}

	public void Heal(float amount)
	{
		if (amount <= 0) return;
		Health = Mathf.Clamp(Health + amount, 0f, MaxHealth);
		EmitSignal(SignalName.HealthChanged, Health, MaxHealth);
	}

	private void PlayAnimation(Vector2 dir)
	{
		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y)) _sprite.Play(dir.X > 0 ? "walk_r" : "walk_l");
		else _sprite.Play("walk_u_d");
	}
}
