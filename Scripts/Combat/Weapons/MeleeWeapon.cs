using Godot;
using System;

namespace Combat.Weapons;

/// <summary>
/// Component that handles melee weapon attacks.
/// Manages weapon sprite, attack hitbox, and directional attacks.
/// Attach as a child of Player or any entity that can melee attack.
/// </summary>
public partial class MeleeWeapon : Node2D
{
	#region Signals
	[Signal] public delegate void AttackStartedEventHandler(AttackDirection direction);
	[Signal] public delegate void AttackHitEventHandler();  // Hitbox becomes active
	[Signal] public delegate void AttackEndedEventHandler();
	#endregion

	#region Configuration
	[Export] public Weapon EquippedWeapon { get; set; }
	
	/// <summary>Path to the AnimatedSprite2D for attack animations (usually the player's sprite).</summary>
	[Export] public NodePath AnimatedSpritePath { get; set; }
	
	/// <summary>Offset for the weapon sprite (adjust to position sword in player's hand).</summary>
	[Export] public Vector2 WeaponSpriteOffset { get; set; } = new Vector2(8, -8);
	
	[ExportGroup("Swing Animation")]
	/// <summary>Starting angle of the swing (degrees).</summary>
	[Export] public float SwingStartAngle { get; set; } = -90f;
	
	/// <summary>Ending angle of the swing (degrees).</summary>
	[Export] public float SwingEndAngle { get; set; } = 45f;
	
	/// <summary>How much the sword moves forward during swing.</summary>
	[Export] public float SwingLungeDistance { get; set; } = 4f;
	#endregion

	#region State
	public bool IsAttacking { get; private set; }
	public AttackDirection CurrentDirection { get; private set; }
	#endregion

	// Components
	private Hitbox _hitbox;
	private CollisionShape2D _hitboxShape;
	private Sprite2D _weaponSprite;
	private AnimatedSprite2D _entitySprite;
	private Node2D _owner;
	
	// Timing
	private float _attackTimer;
	private AttackPhase _currentPhase = AttackPhase.None;
	
	// Swing animation
	private Tween _swingTween;
	
	// Store the original visual setup from the scene (set in editor)
	private Vector2 _originalHitboxPosition;
	private Vector2 _originalShapePosition;
	private float _originalHitboxRotation;

	private enum AttackPhase
	{
		None,
		WindUp,
		Active,
		Recovery
	}

	public override void _Ready()
	{
		_owner = GetParent() as Node2D;
		
		// Find weapon sprite (don't create if not found - user sets it up in scene)
		_weaponSprite = GetNodeOrNull<Sprite2D>("WeaponSprite");
		if (_weaponSprite != null)
		{
			// Apply offset so sword appears in player's hand
			_weaponSprite.Position = WeaponSpriteOffset;
		}
		
		// Find or create hitbox
		SetupHitbox();
		
		// Set hitbox owner to prevent self-damage
		if (_hitbox != null && _owner != null)
		{
			_hitbox.HitboxOwner = _owner;
		}
		
		// Find entity sprite for animations
		if (AnimatedSpritePath != null && !AnimatedSpritePath.IsEmpty)
		{
			_entitySprite = GetNodeOrNull<AnimatedSprite2D>(AnimatedSpritePath);
		}
		else if (_owner != null)
		{
			_entitySprite = _owner.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		}
		
		// Update weapon visuals if weapon is equipped
		if (EquippedWeapon != null)
		{
			UpdateWeaponVisuals();
		}
		
		GD.Print($"MeleeWeapon ready! Hitbox: {_hitbox != null}, Weapon: {EquippedWeapon?.WeaponName ?? "none"}");
	}

	private void SetupHitbox()
	{
		// First, try to find existing Hitbox (Area2D with Hitbox.cs script)
		_hitbox = GetNodeOrNull<Hitbox>("AttackHitbox");
		
		if (_hitbox != null)
		{
			// Found a proper Hitbox, get its shape
			_hitboxShape = _hitbox.GetNodeOrNull<CollisionShape2D>("HitboxShape");
			
			// IMPORTANT: Store the original visual positions set in the editor!
			// These define the "Right" direction attack hitbox
			_originalHitboxPosition = _hitbox.Position;
			_originalHitboxRotation = _hitbox.Rotation;
			if (_hitboxShape != null)
			{
				_originalShapePosition = _hitboxShape.Position;
			}
			
			GD.Print($"Found existing Hitbox - Original pos: {_originalHitboxPosition}, Shape pos: {_originalShapePosition}");
		}
		else
		{
			// Check if there's an Area2D without the script
			var existingArea = GetNodeOrNull<Area2D>("AttackHitbox");
			if (existingArea != null)
			{
				// There's an Area2D but it doesn't have Hitbox.cs - warn the user
				GD.PrintErr("WARNING: AttackHitbox exists but doesn't have Hitbox.cs script attached!");
				GD.PrintErr("Please attach Scripts/Combat/Hitbox.cs to the AttackHitbox node in melee_weapon.tscn");
				
				// Remove it and create a proper one
				existingArea.QueueFree();
			}
			
			// Create a new Hitbox programmatically
			GD.Print("Creating Hitbox programmatically...");
			_hitbox = new Hitbox();
			_hitbox.Name = "AttackHitbox";
			_hitbox.Active = false;
			
			// SET COLLISION LAYERS - This is critical!
			// Layer 4 (bit 3 = value 8) for hitboxes
			// Mask 3 (bit 2 = value 4) to detect hurtboxes
			_hitbox.CollisionLayer = 8;  // Layer 4
			_hitbox.CollisionMask = 4;   // Detect Layer 3 (hurtboxes)
			
			AddChild(_hitbox);
			
			// Create collision shape
			_hitboxShape = new CollisionShape2D();
			_hitboxShape.Name = "HitboxShape";
			_hitboxShape.Disabled = true;
			_hitbox.AddChild(_hitboxShape);
			
			// Store defaults
			_originalHitboxPosition = Vector2.Zero;
			_originalShapePosition = Vector2.Zero;
			_originalHitboxRotation = 0;
			
			GD.Print("Hitbox created with Layer=8, Mask=4");
		}
	}

	public override void _Process(double delta)
	{
		if (!IsAttacking) return;
		if (EquippedWeapon == null) return;
		
		_attackTimer -= (float)delta * EquippedWeapon.AttackSpeed;
		
		switch (_currentPhase)
		{
			case AttackPhase.WindUp:
				if (_attackTimer <= 0)
				{
					// Transition to Active phase
					_currentPhase = AttackPhase.Active;
					_attackTimer = EquippedWeapon.ActiveTime;
					ActivateHitbox();
					EmitSignal(SignalName.AttackHit);
					GD.Print("HITBOX ACTIVATED!");
				}
				break;
				
			case AttackPhase.Active:
				if (_attackTimer <= 0)
				{
					// Transition to Recovery phase
					_currentPhase = AttackPhase.Recovery;
					_attackTimer = EquippedWeapon.RecoveryTime;
					DeactivateHitbox();
					GD.Print("Hitbox deactivated");
				}
				break;
				
			case AttackPhase.Recovery:
				if (_attackTimer <= 0)
				{
					// Attack complete
					EndAttack();
				}
				break;
		}
	}

	/// <summary>
	/// Start a melee attack in the given direction.
	/// Returns true if attack started, false if already attacking or no weapon.
	/// </summary>
	public bool Attack(AttackDirection direction)
	{
		if (IsAttacking) return false;
		if (EquippedWeapon == null) 
		{
			GD.PrintErr("Cannot attack: No weapon equipped!");
			return false;
		}
		
		IsAttacking = true;
		CurrentDirection = direction;
		_currentPhase = AttackPhase.WindUp;
		_attackTimer = EquippedWeapon.WindUpTime;
		
		GD.Print($"Attack started! Direction: {direction}");
		
		// Position hitbox based on direction
		PositionHitbox(direction);
		
		// Update hitbox stats from weapon
		UpdateHitboxStats();
		
		// Play the sword swing animation
		PlaySwingAnimation(direction);
		
		// Play attack animation if available (player sprite)
		PlayAttackAnimation(direction);
		
		EmitSignal(SignalName.AttackStarted, (int)direction);
		
		return true;
	}

	/// <summary>
	/// Convert a Vector2 direction to AttackDirection enum.
	/// Supports 4 directions now, easily expandable to 8 or 16.
	/// </summary>
	public static AttackDirection GetDirectionFromVector(Vector2 dir)
	{
		if (dir == Vector2.Zero) return AttackDirection.Right;
		
		float angle = dir.Angle();
		
		// 4-direction mapping (each direction covers 90 degrees)
		if (angle >= -Mathf.Pi / 4 && angle < Mathf.Pi / 4)
			return AttackDirection.Right;
		else if (angle >= Mathf.Pi / 4 && angle < 3 * Mathf.Pi / 4)
			return AttackDirection.Down;
		else if (angle >= -3 * Mathf.Pi / 4 && angle < -Mathf.Pi / 4)
			return AttackDirection.Up;
		else
			return AttackDirection.Left;
	}

	/// <summary>
	/// Get 8-direction from vector (for future use).
	/// </summary>
	public static AttackDirection8 GetDirection8FromVector(Vector2 dir)
	{
		if (dir == Vector2.Zero) return AttackDirection8.Right;
		
		float angle = dir.Angle();
		float segment = Mathf.Pi / 4;
		
		if (angle < 0) angle += Mathf.Pi * 2;
		
		int index = Mathf.RoundToInt(angle / segment) % 8;
		return (AttackDirection8)index;
	}

	private void PositionHitbox(AttackDirection direction)
	{
		if (_hitbox == null) return;
		
		// Use the VISUAL positions set in the editor!
		// The editor setup defines the "Right" direction attack
		// We transform it for other directions
		
		// Calculate total offset from player center (hitbox pos + shape pos)
		Vector2 totalOffset = _originalHitboxPosition + _originalShapePosition;
		
		// The X component is "forward" distance, Y component is height offset
		float forwardDist = Mathf.Abs(totalOffset.X);  // How far in front (12)
		float heightOffset = totalOffset.Y;            // Vertical offset (-6)
		
		Vector2 finalShapePos;
		
		switch (direction)
		{
			case AttackDirection.Right:
				// Forward is +X, keep height offset
				finalShapePos = new Vector2(forwardDist, heightOffset);
				break;
				
			case AttackDirection.Left:
				// Forward is -X, keep height offset
				finalShapePos = new Vector2(-forwardDist, heightOffset);
				break;
				
			case AttackDirection.Up:
				// Forward is -Y (up in Godot), center horizontally
				finalShapePos = new Vector2(0, -forwardDist);
				break;
				
			case AttackDirection.Down:
				// Forward is +Y (down in Godot), center horizontally
				finalShapePos = new Vector2(0, forwardDist);
				break;
				
			default:
				finalShapePos = totalOffset;
				break;
		}
		
		// Keep hitbox at origin, move the shape
		_hitbox.Position = Vector2.Zero;
		_hitbox.Rotation = 0;
		
		if (_hitboxShape != null)
		{
			_hitboxShape.Position = finalShapePos;
		}
	}

	private void UpdateHitboxStats()
	{
		if (_hitbox == null || EquippedWeapon == null) return;
		
		_hitbox.BaseDamage = EquippedWeapon.BaseDamage;
		_hitbox.DamageType = EquippedWeapon.DamageType;
		_hitbox.KnockbackForce = EquippedWeapon.KnockbackForce;
		_hitbox.PoiseDamage = EquippedWeapon.PoiseDamage;
		_hitbox.CanCrit = EquippedWeapon.CanCrit;
	}

	private void ActivateHitbox()
	{
		if (_hitbox == null) return;
		if (_hitboxShape != null) _hitboxShape.Disabled = false;
		_hitbox.Activate();
		_hitbox.CheckHitsNow(); // Immediately check for overlaps
	}

	private void DeactivateHitbox()
	{
		if (_hitbox == null) return;
		if (_hitboxShape != null) _hitboxShape.Disabled = true;
		_hitbox.Deactivate();
	}

	private void PlaySwingAnimation(AttackDirection direction)
	{
		if (_weaponSprite == null) return;
		if (EquippedWeapon == null) return;
		
		// Kill any existing swing tween
		_swingTween?.Kill();
		
		// Calculate total swing duration (wind-up + active)
		float swingDuration = (EquippedWeapon.WindUpTime + EquippedWeapon.ActiveTime) / EquippedWeapon.AttackSpeed;
		
		// Get direction-specific values
		float baseRotation = GetBaseRotationForDirection(direction);
		float startAngle = Mathf.DegToRad(SwingStartAngle);
		float endAngle = Mathf.DegToRad(SwingEndAngle);
		Vector2 basePos = GetBasePositionForDirection(direction);
		Vector2 lungeDir = GetLungeDirectionForDirection(direction);
		bool flipH = (direction == AttackDirection.Left);
		
		// If attacking left, mirror the swing angles
		if (direction == AttackDirection.Left)
		{
			startAngle = -startAngle;
			endAngle = -endAngle;
		}
		// If attacking up/down, adjust angles
		if (direction == AttackDirection.Up || direction == AttackDirection.Down)
		{
			startAngle = Mathf.DegToRad(direction == AttackDirection.Up ? -45f : 45f);
			endAngle = Mathf.DegToRad(direction == AttackDirection.Up ? 45f : -45f);
		}
		
		// Set initial state
		_weaponSprite.Position = basePos;
		_weaponSprite.Rotation = baseRotation + startAngle;
		_weaponSprite.FlipH = flipH;
		_weaponSprite.Visible = true;
		
		// Create swing tween
		_swingTween = CreateTween();
		_swingTween.SetEase(Tween.EaseType.Out);
		_swingTween.SetTrans(Tween.TransitionType.Cubic);
		
		// Animate rotation (the swing arc)
		_swingTween.TweenProperty(_weaponSprite, "rotation", baseRotation + endAngle, swingDuration);
		
		// Animate position (lunge forward slightly) - parallel with rotation
		_swingTween.Parallel().TweenProperty(
			_weaponSprite, 
			"position", 
			basePos + lungeDir * SwingLungeDistance, 
			swingDuration * 0.5f
		);
		
		// Then return position
		_swingTween.TweenProperty(
			_weaponSprite, 
			"position", 
			basePos, 
			swingDuration * 0.5f
		);
	}
	
	private float GetBaseRotationForDirection(AttackDirection direction)
	{
		return direction switch
		{
			AttackDirection.Right => 0,
			AttackDirection.Left => 0,
			AttackDirection.Up => -Mathf.Pi / 2,
			AttackDirection.Down => Mathf.Pi / 2,
			_ => 0
		};
	}
	
	private Vector2 GetBasePositionForDirection(AttackDirection direction)
	{
		return direction switch
		{
			AttackDirection.Right => new Vector2(Mathf.Abs(WeaponSpriteOffset.X), WeaponSpriteOffset.Y),
			AttackDirection.Left => new Vector2(-Mathf.Abs(WeaponSpriteOffset.X), WeaponSpriteOffset.Y),
			AttackDirection.Up => new Vector2(0, WeaponSpriteOffset.Y - 4),
			AttackDirection.Down => new Vector2(0, -WeaponSpriteOffset.Y + 8),
			_ => WeaponSpriteOffset
		};
	}
	
	private Vector2 GetLungeDirectionForDirection(AttackDirection direction)
	{
		return direction switch
		{
			AttackDirection.Right => Vector2.Right,
			AttackDirection.Left => Vector2.Left,
			AttackDirection.Up => Vector2.Up,
			AttackDirection.Down => Vector2.Down,
			_ => Vector2.Right
		};
	}

	private void PlayAttackAnimation(AttackDirection direction)
	{
		if (_entitySprite == null || EquippedWeapon == null) return;
		
		string animName = $"{EquippedWeapon.AttackAnimationPrefix}_{DirectionToString(direction)}";
		
		if (_entitySprite.SpriteFrames != null && 
			_entitySprite.SpriteFrames.HasAnimation(animName))
		{
			_entitySprite.Play(animName);
			_entitySprite.SpeedScale = EquippedWeapon.AttackSpeed;
		}
	}

	private void EndAttack()
	{
		IsAttacking = false;
		_currentPhase = AttackPhase.None;
		DeactivateHitbox();
		
		// Stop swing animation
		_swingTween?.Kill();
		
		// Reset weapon sprite to default position
		if (_weaponSprite != null)
		{
			_weaponSprite.Position = WeaponSpriteOffset;
			_weaponSprite.Rotation = 0;
			_weaponSprite.FlipH = false;
		}
		
		// Reset hitbox to original visual position (what you set in editor)
		if (_hitbox != null)
		{
			_hitbox.Position = Vector2.Zero;
			_hitbox.Rotation = 0;
		}
		if (_hitboxShape != null)
		{
			// Reset to original combined position for "Right" direction
			_hitboxShape.Position = _originalHitboxPosition + _originalShapePosition;
		}
		
		// Reset animation speed
		if (_entitySprite != null)
		{
			_entitySprite.SpeedScale = 1.0f;
		}
		
		EmitSignal(SignalName.AttackEnded);
	}

	private void UpdateWeaponVisuals()
	{
		if (_weaponSprite == null) return;
		
		// Only update texture from weapon resource if weapon has a sprite defined
		// Don't hide the sprite if it already has a texture from the scene!
		if (EquippedWeapon?.WeaponSprite != null)
		{
			_weaponSprite.Texture = EquippedWeapon.WeaponSprite;
		}
		// If weapon doesn't define a sprite, keep whatever is in the scene
		
		// Make sure sprite is visible
		_weaponSprite.Visible = true;
		_weaponSprite.Position = WeaponSpriteOffset;
	}

	/// <summary>
	/// Equip a new weapon.
	/// </summary>
	public void EquipWeapon(Weapon weapon)
	{
		EquippedWeapon = weapon;
		UpdateWeaponVisuals();
	}

	private static string DirectionToString(AttackDirection dir)
	{
		return dir switch
		{
			AttackDirection.Right => "right",
			AttackDirection.Left => "left",
			AttackDirection.Up => "up",
			AttackDirection.Down => "down",
			_ => "right"
		};
	}
}

/// <summary>
/// 4-direction attack enum.
/// </summary>
public enum AttackDirection
{
	Right = 0,
	Down = 1,
	Left = 2,
	Up = 3
}

/// <summary>
/// 8-direction attack enum (for future expansion).
/// </summary>
public enum AttackDirection8
{
	Right = 0,
	DownRight = 1,
	Down = 2,
	DownLeft = 3,
	Left = 4,
	UpLeft = 5,
	Up = 6,
	UpRight = 7
}
