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
			GD.Print("Found existing Hitbox with script");
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
		
		// Rotate weapon sprite to match attack direction
		UpdateWeaponSpriteDirection(direction);
		
		// Play attack animation if available
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
		if (_hitbox == null || EquippedWeapon == null) return;
		
		float distance = EquippedWeapon.HitboxDistance;
		Vector2 size = EquippedWeapon.HitboxSize;
		
		// Position and rotate hitbox based on direction
		switch (direction)
		{
			case AttackDirection.Right:
				_hitbox.Position = new Vector2(distance, 0);
				_hitbox.Rotation = 0;
				break;
			case AttackDirection.Left:
				_hitbox.Position = new Vector2(-distance, 0);
				_hitbox.Rotation = Mathf.Pi;
				break;
			case AttackDirection.Up:
				_hitbox.Position = new Vector2(0, -distance);
				_hitbox.Rotation = -Mathf.Pi / 2;
				break;
			case AttackDirection.Down:
				_hitbox.Position = new Vector2(0, distance);
				_hitbox.Rotation = Mathf.Pi / 2;
				break;
		}
		
		// Update hitbox shape size
		if (_hitboxShape != null)
		{
			var rect = new RectangleShape2D();
			rect.Size = size;
			_hitboxShape.Shape = rect;
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

	private void UpdateWeaponSpriteDirection(AttackDirection direction)
	{
		if (_weaponSprite == null) return;
		
		// Rotate and flip weapon sprite based on direction
		switch (direction)
		{
			case AttackDirection.Right:
				_weaponSprite.Position = new Vector2(Mathf.Abs(WeaponSpriteOffset.X), WeaponSpriteOffset.Y);
				_weaponSprite.FlipH = false;
				_weaponSprite.Rotation = 0;
				break;
			case AttackDirection.Left:
				_weaponSprite.Position = new Vector2(-Mathf.Abs(WeaponSpriteOffset.X), WeaponSpriteOffset.Y);
				_weaponSprite.FlipH = true;
				_weaponSprite.Rotation = 0;
				break;
			case AttackDirection.Up:
				_weaponSprite.Position = new Vector2(0, WeaponSpriteOffset.Y - 4);
				_weaponSprite.FlipH = false;
				_weaponSprite.Rotation = -Mathf.Pi / 2;
				break;
			case AttackDirection.Down:
				_weaponSprite.Position = new Vector2(0, -WeaponSpriteOffset.Y + 4);
				_weaponSprite.FlipH = false;
				_weaponSprite.Rotation = Mathf.Pi / 2;
				break;
		}
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
		
		// Reset weapon sprite to default position
		if (_weaponSprite != null)
		{
			_weaponSprite.Position = WeaponSpriteOffset;
			_weaponSprite.Rotation = 0;
			_weaponSprite.FlipH = false;
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
