using Godot;

namespace Combat.StatusEffects;

/// <summary>
/// Example status effect: Burn
/// Deals fire damage over time.
/// </summary>
[GlobalClass]
public partial class BurnEffect : StatusEffect
{
    [Export] public float DamagePerSecond { get; set; } = 5f;
    [Export] public float TickInterval { get; set; } = 0.5f;
    
    private float _tickTimer;
    private Node2D _source;

    public void SetSource(Node2D source)
    {
        _source = source;
    }

    public override void OnApply(Node2D target, CombatStats stats)
    {
        _tickTimer = 0f;
        GD.Print($"{target.Name} is now burning!");
    }

    public override void OnTick(Node2D target, CombatStats stats, float delta)
    {
        _tickTimer += delta;
        
        if (_tickTimer >= TickInterval)
        {
            _tickTimer -= TickInterval;
            
            // Deal fire damage (scaled by stacks)
            float damage = DamagePerSecond * TickInterval * CurrentStacks;
            var damageInfo = DamageInfo.Elemental(damage, DamageType.Fire, _source, Vector2.Zero, 0f);
            damageInfo.PoiseDamage = 0; // DoT shouldn't stagger
            stats.ProcessDamage(ref damageInfo);
        }
    }

    public override void OnExpire(Node2D target, CombatStats stats)
    {
        GD.Print($"{target.Name} is no longer burning.");
    }
}

