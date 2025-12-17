using Godot;
using System;

public partial class Player : CharacterBody2D
{
	private Hud _hud;
	[Signal] public delegate void SkillActivatedEventHandler(int slot, float cooldown);

	[Export] public float MoveSpeed = 60f;
	[Export] public Skill[] Skills = new Skill[2]; // Slot 0 and Slot 1

	private AnimatedSprite2D _sprite;
	
	// State machine variables
	private enum State { Idle, Dashing, Teleporting }
	private State _currentState = State.Idle;
	private double _stateTimer;
	
	// Dash/TP specific
	private Vector2 _velocityOverride;
	private bool _tpTriggered;
	private Vector2 _tpTarget;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		// Get reference to HUD
		_hud = GetNode<Hud>("../level0/HUD");
		
		// Connect the signal
		SkillActivated += _hud.OnPlayerSkillActivated;
		GD.Print("HUD connected successfully!");
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
				if (_tpTriggered && _stateTimer <= 0.5) { GlobalPosition = _tpTarget; _tpTriggered = false; }
				if (_stateTimer <= 0) _currentState = State.Idle;
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

	public void ApplyTeleport(Vector2 dir, float dist, float time)
	{
		_currentState = State.Teleporting;
		_stateTimer = time;
		_tpTriggered = true;
		_tpTarget = GlobalPosition + (dir * dist);
		_sprite.Play("teleport");
	}

	private void PlayAnimation(Vector2 dir)
	{
		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y)) _sprite.Play(dir.X > 0 ? "walk_r" : "walk_l");
		else _sprite.Play("walk_u_d");
	}
}
