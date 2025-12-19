using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	private AnimatedSprite2D _sprite;
	private float _attackCooldown;

	// Generalized skill resource (assign Fireball.tres here)
	[Export] public Skill AttackSkill;
	// Fixed direction to fire (e.g. (1,0) right, (-1,0) left, (0,-1) up, (0,1) down)
	[Export] public Vector2 FireDirection = Vector2.Left;
	
	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("idle");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (AttackSkill == null) return;

		_attackCooldown = Mathf.Max(0, _attackCooldown - (float)delta);
		if (_attackCooldown > 0) return;

		var dir = FireDirection.Normalized();
		if (dir == Vector2.Zero) return;

		if (AttackSkill.Execute(this, dir))
		{
			_attackCooldown = AttackSkill.Cooldown;
		}
	}
}
