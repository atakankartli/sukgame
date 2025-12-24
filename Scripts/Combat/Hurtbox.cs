using Godot;
using System;

namespace Combat;

/// <summary>
/// Attach to an Area2D to make it receive damage from Hitboxes.
/// The parent (or a specified node) must have a CombatStats component.
/// </summary>
[GlobalClass]
public partial class Hurtbox : Area2D
{
	#region C# Events (for complex types)
	/// <summary>C# event fired when hurt.</summary>
	public event Action<DamageInfo> Hurt;
	#endregion
	
	/// <summary>
	/// Path to the CombatStats node. If empty, searches parent hierarchy.
	/// </summary>
	[Export] public NodePath CombatStatsPath { get; set; }
	
	/// <summary>
	/// If true, this hurtbox is currently active and can receive damage.
	/// </summary>
	[Export] public bool Active { get; set; } = true;
	
	private CombatStats _stats;

	public override void _Ready()
	{
		_stats = FindCombatStats();
		if (_stats == null)
		{
			GD.PrintErr($"Hurtbox '{Name}': Could not find CombatStats component!");
		}
	}

	private CombatStats FindCombatStats()
	{
		// 1. Try explicit path
		if (CombatStatsPath != null && !CombatStatsPath.IsEmpty)
		{
			return GetNodeOrNull<CombatStats>(CombatStatsPath);
		}
		
		// 2. Search up the parent hierarchy
		Node current = GetParent();
		while (current != null)
		{
			// Check if parent has CombatStats as a child
			var stats = current.GetNodeOrNull<CombatStats>("CombatStats");
			if (stats != null) return stats;
			
			// Check if parent IS CombatStats (unlikely but possible)
			if (current is CombatStats cs) return cs;
			
			current = current.GetParent();
		}
		
		return null;
	}

	/// <summary>
	/// Called by Hitbox when it overlaps this Hurtbox.
	/// </summary>
	public float ReceiveDamage(DamageInfo info)
	{
		if (!Active) return 0f;
		if (_stats == null) return 0f;
		if (!_stats.IsAlive) return 0f;
		
		float damage = _stats.ProcessDamage(ref info);
		
		if (damage > 0)
		{
			Hurt?.Invoke(info);
		}
		
		return damage;
	}

	/// <summary>
	/// Get the owner entity (the CharacterBody2D or similar that owns this hurtbox).
	/// </summary>
	public Node2D GetOwnerEntity()
	{
		Node current = GetParent();
		while (current != null)
		{
			if (current is CharacterBody2D cb) return cb;
			if (current is RigidBody2D rb) return rb;
			if (current is StaticBody2D sb) return sb;
			current = current.GetParent();
		}
		return GetParent() as Node2D;
	}
}
