using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Combat.StatusEffects;

/// <summary>
/// Manages active status effects on an entity.
/// Attach as a child of an entity with CombatStats.
/// </summary>
public partial class StatusEffectManager : Node
{
    #region C# Events (for complex types)
    /// <summary>C# event fired when an effect is applied.</summary>
    public event Action<StatusEffect> EffectApplied;
    
    /// <summary>C# event fired when an effect is removed.</summary>
    public event Action<StatusEffect> EffectRemoved;
    
    /// <summary>C# event fired when an effect is stacked.</summary>
    public event Action<StatusEffect, int> EffectStacked;
    #endregion

    private CombatStats _stats;
    private Node2D _owner;
    private List<StatusEffect> _activeEffects = new();

    public IReadOnlyList<StatusEffect> ActiveEffects => _activeEffects;

    public override void _Ready()
    {
        // Find owner and stats
        _owner = GetParent() as Node2D;
        if (_owner == null)
        {
            Node current = GetParent();
            while (current != null)
            {
                if (current is Node2D n2d)
                {
                    _owner = n2d;
                    break;
                }
                current = current.GetParent();
            }
        }
        
        _stats = GetParent().GetNodeOrNull<CombatStats>("CombatStats");
        if (_stats == null)
        {
            _stats = GetNodeOrNull<CombatStats>("../CombatStats");
        }
        
        if (_stats == null)
        {
            GD.PrintErr($"StatusEffectManager '{Name}': Could not find CombatStats!");
        }
    }

    public override void _Process(double delta)
    {
        if (_stats == null || !_stats.IsAlive) return;
        
        float dt = (float)delta;
        var toRemove = new List<StatusEffect>();
        
        foreach (var effect in _activeEffects)
        {
            // Tick the effect
            effect.OnTick(_owner, _stats, dt);
            
            // Update duration (negative duration = infinite)
            if (effect.Duration >= 0)
            {
                effect.TimeRemaining -= dt;
                if (effect.TimeRemaining <= 0)
                {
                    toRemove.Add(effect);
                }
            }
        }
        
        // Remove expired effects
        foreach (var effect in toRemove)
        {
            RemoveEffect(effect);
        }
    }

    /// <summary>
    /// Apply a status effect. Handles stacking and refresh logic.
    /// </summary>
    public void ApplyEffect(StatusEffect effectTemplate)
    {
        if (effectTemplate == null) return;
        if (_stats == null || !_stats.IsAlive) return;
        
        // Check if we already have this effect
        var existing = _activeEffects.FirstOrDefault(e => e.EffectName == effectTemplate.EffectName);
        
        if (existing != null)
        {
            if (effectTemplate.Stackable)
            {
                existing.OnStack(_owner, _stats, 1);
                EffectStacked?.Invoke(existing, existing.CurrentStacks);
            }
            else if (effectTemplate.RefreshOnReapply)
            {
                existing.TimeRemaining = existing.Duration;
            }
            return;
        }
        
        // Create new instance and apply
        var instance = effectTemplate.CreateInstance();
        _activeEffects.Add(instance);
        instance.OnApply(_owner, _stats);
        EffectApplied?.Invoke(instance);
    }

    /// <summary>
    /// Remove a specific effect instance.
    /// </summary>
    public void RemoveEffect(StatusEffect effect)
    {
        if (!_activeEffects.Contains(effect)) return;
        
        effect.OnExpire(_owner, _stats);
        _activeEffects.Remove(effect);
        EffectRemoved?.Invoke(effect);
    }

    /// <summary>
    /// Remove all effects with a given name.
    /// </summary>
    public void RemoveEffectByName(string effectName)
    {
        var toRemove = _activeEffects.Where(e => e.EffectName == effectName).ToList();
        foreach (var effect in toRemove)
        {
            RemoveEffect(effect);
        }
    }

    /// <summary>
    /// Remove all active effects.
    /// </summary>
    public void ClearAllEffects()
    {
        foreach (var effect in _activeEffects.ToList())
        {
            RemoveEffect(effect);
        }
    }

    /// <summary>
    /// Check if an effect with the given name is currently active.
    /// </summary>
    public bool HasEffect(string effectName)
    {
        return _activeEffects.Any(e => e.EffectName == effectName);
    }

    /// <summary>
    /// Get the current stack count of an effect.
    /// </summary>
    public int GetStacks(string effectName)
    {
        var effect = _activeEffects.FirstOrDefault(e => e.EffectName == effectName);
        return effect?.CurrentStacks ?? 0;
    }
}
