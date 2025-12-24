using Godot;

namespace Combat.StatusEffects;

/// <summary>
/// Base class for all status effects (buffs, debuffs, DoTs, etc.).
/// Extend this to create specific effects like Burn, Freeze, Poison, etc.
/// </summary>
[GlobalClass]
public abstract partial class StatusEffect : Resource
{
    [Export] public string EffectName { get; set; } = "Effect";
    [Export] public Texture2D Icon { get; set; }
    
    /// <summary>Duration in seconds. Negative = infinite until manually removed.</summary>
    [Export] public float Duration { get; set; } = 5f;
    
    /// <summary>Can this effect stack with itself?</summary>
    [Export] public bool Stackable { get; set; } = false;
    
    /// <summary>Maximum stacks if Stackable is true.</summary>
    [Export] public int MaxStacks { get; set; } = 1;
    
    /// <summary>If the same effect is applied again, refresh duration?</summary>
    [Export] public bool RefreshOnReapply { get; set; } = true;
    
    // Runtime state (not exported - set by manager)
    public int CurrentStacks { get; set; } = 1;
    public float TimeRemaining { get; set; }
    
    /// <summary>
    /// Called when the effect is first applied.
    /// </summary>
    public abstract void OnApply(Node2D target, CombatStats stats);
    
    /// <summary>
    /// Called every frame while the effect is active.
    /// </summary>
    public abstract void OnTick(Node2D target, CombatStats stats, float delta);
    
    /// <summary>
    /// Called when the effect expires or is removed.
    /// </summary>
    public abstract void OnExpire(Node2D target, CombatStats stats);
    
    /// <summary>
    /// Called when another instance of the same effect is applied (for stacking).
    /// </summary>
    public virtual void OnStack(Node2D target, CombatStats stats, int newStacks)
    {
        CurrentStacks = Mathf.Min(CurrentStacks + newStacks, MaxStacks);
        if (RefreshOnReapply)
        {
            TimeRemaining = Duration;
        }
    }
    
    /// <summary>
    /// Creates a runtime instance of this effect.
    /// </summary>
    public StatusEffect CreateInstance()
    {
        var instance = (StatusEffect)Duplicate();
        instance.TimeRemaining = instance.Duration;
        instance.CurrentStacks = 1;
        return instance;
    }
}

