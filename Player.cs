using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 60f;
	[Export] public float DashSpeed = 300f;
	[Export] public float DashTime = 0.2f;
	[Export] public float DashCooldown = 1.0f;

	[Export] public float TeleportDistance = 200f;
	[Export] public float TeleportDuration = 1.0f;    // 1 second teleport
	[Export] public float TeleportCooldown = 2.0f;

	private AnimatedSprite2D _sprite;

	private bool _isDashing = false;
	private double _dashTimer = 0;
	private double _cooldownTimer = 0;

	private bool _isTeleporting = false;
	private bool _isTeleportHalfway = false;
	private double _teleportTimer = 0;
	private double _teleportCooldownTimer = 0;
	private Vector2 _teleportDirection = Vector2.Zero;
	private Vector2 _teleportStartPos = Vector2.Zero;

	private Vector2 _dashDirection = Vector2.Zero;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		// Update timers
		if (_cooldownTimer > 0) _cooldownTimer -= delta;
		if (_teleportCooldownTimer > 0) _teleportCooldownTimer -= delta;
		

		// Handle teleporting
		if (_isTeleporting)
		{
			_teleportTimer -= delta;

			// Play teleport animation
			_sprite.Play("teleport"); // make sure your AnimatedSprite2D has a "teleport" animation
			
			
			if (_teleportTimer <= TeleportDuration / 2)
			{
				if(_isTeleportHalfway) 
				{
					// Move player to target position
					GlobalPosition = _teleportStartPos + _teleportDirection * TeleportDistance;
					_isTeleportHalfway = false;
				}
				if (_teleportTimer <= 0)
				{
					_isTeleporting = false;
					_teleportCooldownTimer = TeleportCooldown;
					GD.Print("Teleport complete!");
				}
			}

			Velocity = Vector2.Zero; // freeze player during teleport
			return;
		}

		// Handle dash
		if (_isDashing)
		{
			_dashTimer -= delta;
			Velocity = _dashDirection * DashSpeed;

			if (_dashTimer <= 0) _isDashing = false;

			MoveAndSlide();
			return;
		}

		// Normal movement
		Vector2 dir = Vector2.Zero;
		dir.X = Input.GetActionStrength("ui_right") - Input.GetActionStrength("ui_left");
		dir.Y = Input.GetActionStrength("ui_down") - Input.GetActionStrength("ui_up");

		if (dir != Vector2.Zero)
		{
			dir = dir.Normalized();
			Velocity = dir * Speed;
			PlayAnimation(dir);
		}
		else
		{
			Velocity = Vector2.Zero;
			_sprite.Play("idle");
		}

		MoveAndSlide();

		// Dash input
		if (Input.IsKeyPressed(Key.E) && _cooldownTimer <= 0 && dir != Vector2.Zero)
		{
			_isDashing = true;
			_dashTimer = DashTime;
			_cooldownTimer = DashCooldown;
			_dashDirection = dir;
			GD.Print("Dashing!");
		}

		// Teleport input
		if (Input.IsKeyPressed(Key.Q) && _teleportCooldownTimer <= 0 && dir != Vector2.Zero)
		{
			StartTeleport(dir);
		}
	}

	private void StartTeleport(Vector2 dir)
	{
		dir = dir.Normalized();
		_isTeleporting = true;
		_isTeleportHalfway = true;
		_teleportTimer = TeleportDuration;
		_teleportDirection = dir;
		_teleportStartPos = GlobalPosition;

		GD.Print("Teleport started!");
	}

	private void PlayAnimation(Vector2 dir)
	{
		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
		{
			_sprite.Play(dir.X > 0 ? "walk_r" : "walk_l");
		}
		else
		{
			_sprite.Play("walk_u_d");
		}
	}
}
