using Godot;
using System;
using System.Collections.Generic;

namespace Combat;

/// <summary>
/// Attach to an Area2D to make it deal damage to Hurtboxes.
/// Used for melee attacks, projectiles, hazards, etc.
/// </summary>
[GlobalClass]
public partial class Hitbox : Area2D
{
    #region C# Events (for complex types)
    /// <summary>C# event fired when a hit is confirmed.</summary>
    public event Action<Hurtbox, DamageInfo> HitConfirmed;
    #endregion
    
    #region Configuration
    [ExportGroup("Damage")]
    [Export] public float BaseDamage { get; set; } = 10f;
    [Export] public DamageType DamageType { get; set; } = DamageType.Physical;
    [Export] public float KnockbackForce { get; set; } = 100f;
    [Export] public float PoiseDamage { get; set; } = 10f;
    [Export] public bool CanCrit { get; set; } = true;
    
    [ExportGroup("Behavior")]
    /// <summary>If true, the hitbox is currently checking for hits.</summary>
    [Export] public bool Active { get; set; } = false;
    
    /// <summary>If true, can hit the same target multiple times (e.g., multi-hit attacks).</summary>
    [Export] public bool AllowMultiHit { get; set; } = false;
    
    /// <summary>Time between multi-hits on the same target.</summary>
    [Export] public float MultiHitCooldown { get; set; } = 0.5f;
    #endregion

    /// <summary>The entity that owns this hitbox (set this to prevent self-damage).</summary>
    public Node2D HitboxOwner { get; set; }
    
    /// <summary>Override the attack direction (if not set, calculates from positions).</summary>
    public Vector2? DirectionOverride { get; set; }
    
    // Track what we've already hit to prevent double-hits
    private Dictionary<Hurtbox, float> _hitTargets = new();

    public override void _Ready()
    {
        // Connect to area detection
        AreaEntered += OnAreaEntered;
    }

    public override void _Process(double delta)
    {
        if (!Active || !AllowMultiHit) return;
        
        // Update multi-hit cooldowns
        var toRemove = new List<Hurtbox>();
        var keys = new List<Hurtbox>(_hitTargets.Keys);
        
        foreach (var target in keys)
        {
            _hitTargets[target] -= (float)delta;
            if (_hitTargets[target] <= 0)
            {
                toRemove.Add(target);
            }
        }
        
        foreach (var target in toRemove)
        {
            _hitTargets.Remove(target);
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        if (!Active) return;
        if (area is not Hurtbox hurtbox) return;
        if (!hurtbox.Active) return;
        
        // Don't hit ourselves
        var targetOwner = hurtbox.GetOwnerEntity();
        if (HitboxOwner != null && targetOwner == HitboxOwner) return;
        
        // Check if we already hit this target
        if (_hitTargets.ContainsKey(hurtbox))
        {
            if (!AllowMultiHit) return;
            if (_hitTargets[hurtbox] > 0) return;
        }
        
        // Calculate direction
        Vector2 direction = DirectionOverride ?? 
            (hurtbox.GlobalPosition - GlobalPosition).Normalized();
        
        if (direction == Vector2.Zero)
            direction = Vector2.Right;
        
        // Build damage info
        var damageInfo = new DamageInfo
        {
            BaseDamage = BaseDamage,
            Source = HitboxOwner,
            Direction = direction,
            Type = DamageType,
            KnockbackForce = KnockbackForce,
            PoiseDamage = PoiseDamage,
            CanCrit = CanCrit
        };
        
        // Deal damage
        float dealt = hurtbox.ReceiveDamage(damageInfo);
        
        if (dealt > 0)
        {
            // Track this hit
            _hitTargets[hurtbox] = AllowMultiHit ? MultiHitCooldown : float.MaxValue;
            
            // Fire C# event for effects, sounds, etc.
            HitConfirmed?.Invoke(hurtbox, damageInfo);
        }
    }

    /// <summary>
    /// Activate the hitbox (start checking for hits).
    /// </summary>
    public void Activate()
    {
        Active = true;
        _hitTargets.Clear();
    }

    /// <summary>
    /// Deactivate the hitbox (stop checking for hits).
    /// </summary>
    public void Deactivate()
    {
        Active = false;
    }

    /// <summary>
    /// Clear hit memory (allows hitting the same targets again).
    /// </summary>
    public void ResetHits()
    {
        _hitTargets.Clear();
    }

    /// <summary>
    /// Perform an immediate check for overlapping hurtboxes (useful for instant attacks).
    /// </summary>
    public void CheckHitsNow()
    {
        if (!Active) return;
        
        foreach (var area in GetOverlappingAreas())
        {
            OnAreaEntered(area);
        }
    }
}
