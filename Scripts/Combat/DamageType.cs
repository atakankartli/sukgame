namespace Combat;

/// <summary>
/// Defines the type of damage being dealt.
/// Used for resistances, immunities, and visual feedback.
/// </summary>
public enum DamageType
{
    Physical,   // Melee weapons, arrows
    Fire,       // Burns, explosions
    Ice,        // Freezing attacks
    Lightning,  // Shock damage
    Poison,     // Toxic damage
    Holy,       // Divine damage
    Dark,       // Cursed damage
    True        // Ignores all defenses
}

