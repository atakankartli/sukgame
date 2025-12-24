using Godot;

namespace Combat;

/// <summary>
/// Carries all information about a damage instance.
/// Passed through the damage pipeline for processing.
/// </summary>
public struct DamageInfo
{
    /// <summary>Raw damage before any modifiers.</summary>
    public float BaseDamage;
    
    /// <summary>The node that caused this damage (for knockback direction, kill credit, etc.).</summary>
    public Node2D Source;
    
    /// <summary>Direction the attack came from (normalized). Used for knockback.</summary>
    public Vector2 Direction;
    
    /// <summary>Type of damage for resistances and effects.</summary>
    public DamageType Type;
    
    /// <summary>Knockback force multiplier. 0 = no knockback.</summary>
    public float KnockbackForce;
    
    /// <summary>Poise damage. When poise breaks, target staggers.</summary>
    public float PoiseDamage;
    
    /// <summary>Whether this attack can critically hit.</summary>
    public bool CanCrit;
    
    /// <summary>If true, this damage instance was a critical hit (set during processing).</summary>
    public bool WasCrit;
    
    /// <summary>Final damage after all modifiers (set during processing).</summary>
    public float FinalDamage;

    /// <summary>
    /// Creates a basic physical damage instance.
    /// </summary>
    public static DamageInfo Physical(float damage, Node2D source, Vector2 direction, float knockback = 100f)
    {
        return new DamageInfo
        {
            BaseDamage = damage,
            Source = source,
            Direction = direction.Normalized(),
            Type = DamageType.Physical,
            KnockbackForce = knockback,
            PoiseDamage = damage * 0.5f, // Default: poise damage = half of base damage
            CanCrit = true
        };
    }
    
    /// <summary>
    /// Creates an elemental damage instance.
    /// </summary>
    public static DamageInfo Elemental(float damage, DamageType type, Node2D source, Vector2 direction, float knockback = 50f)
    {
        return new DamageInfo
        {
            BaseDamage = damage,
            Source = source,
            Direction = direction.Normalized(),
            Type = type,
            KnockbackForce = knockback,
            PoiseDamage = damage * 0.25f, // Elemental attacks do less poise damage
            CanCrit = false
        };
    }
}

