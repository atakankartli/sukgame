namespace Combat;

/// <summary>
/// Interface for any entity that can receive damage.
/// Implement this on Player, Enemy, destructible objects, etc.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Process incoming damage. Returns the actual damage dealt after modifiers.
    /// </summary>
    float TakeDamage(DamageInfo damageInfo);
    
    /// <summary>
    /// Whether this entity is still alive.
    /// </summary>
    bool IsAlive { get; }
    
    /// <summary>
    /// Current health value.
    /// </summary>
    float CurrentHealth { get; }
    
    /// <summary>
    /// Maximum health value.
    /// </summary>
    float MaxHealth { get; }
}

