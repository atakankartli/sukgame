using Godot;

namespace Combat.Weapons;

/// <summary>
/// Base resource for all weapons. Contains stats and configuration.
/// Create .tres files for each weapon type (Sword.tres, Axe.tres, etc.)
/// </summary>
[GlobalClass]
public partial class Weapon : Resource
{
    #region Basic Info
    [ExportGroup("Info")]
    [Export] public string WeaponName { get; set; } = "Weapon";
    [Export] public Texture2D Icon { get; set; }
    #endregion

    #region Damage Stats
    [ExportGroup("Damage")]
    [Export] public float BaseDamage { get; set; } = 10f;
    [Export] public DamageType DamageType { get; set; } = DamageType.Physical;
    [Export] public float KnockbackForce { get; set; } = 80f;
    [Export] public float PoiseDamage { get; set; } = 15f;
    [Export] public bool CanCrit { get; set; } = true;
    #endregion

    #region Attack Properties
    [ExportGroup("Attack")]
    /// <summary>Attack speed multiplier. 1.0 = normal, 2.0 = twice as fast.</summary>
    [Export] public float AttackSpeed { get; set; } = 1.0f;
    
    /// <summary>Stamina cost per attack (for future stamina system).</summary>
    [Export] public float StaminaCost { get; set; } = 20f;
    
    /// <summary>Time in seconds before hitbox activates (wind-up).</summary>
    [Export] public float WindUpTime { get; set; } = 0.1f;
    
    /// <summary>Time in seconds the hitbox stays active.</summary>
    [Export] public float ActiveTime { get; set; } = 0.15f;
    
    /// <summary>Time in seconds after attack before you can act again.</summary>
    [Export] public float RecoveryTime { get; set; } = 0.2f;
    
    /// <summary>Total attack duration = WindUp + Active + Recovery</summary>
    public float TotalAttackTime => WindUpTime + ActiveTime + RecoveryTime;
    #endregion

    #region Hitbox Configuration
    [ExportGroup("Hitbox")]
    /// <summary>Size of the attack hitbox.</summary>
    [Export] public Vector2 HitboxSize { get; set; } = new Vector2(24, 16);
    
    /// <summary>How far in front of the player the hitbox spawns.</summary>
    [Export] public float HitboxDistance { get; set; } = 16f;
    #endregion

    #region Visuals
    [ExportGroup("Visuals")]
    /// <summary>Sprite sheet for the weapon.</summary>
    [Export] public Texture2D WeaponSprite { get; set; }
    
    /// <summary>Animation frames for each direction. Format: "attack_right", "attack_up", etc.</summary>
    [Export] public string AttackAnimationPrefix { get; set; } = "attack";
    #endregion
}

