using Godot;

[GlobalClass]
public partial class SkillFireball : Skill
{
	[Export] public PackedScene FireballScene;
	[Export] public float Speed = 260f;
	[Export] public float Damage = 10f;
	[Export] public float LifetimeSeconds = 1.5f;

	public override bool Execute(Node2D caster, Vector2 direction)
	{
		if (caster == null) return false;
		if (direction == Vector2.Zero) return false;
		if (FireballScene == null) return false;

		var instance = FireballScene.Instantiate();
		if (instance is not Fireball fireball) return false;

		caster.GetTree().CurrentScene.AddChild(fireball);
		fireball.GlobalPosition = caster.GlobalPosition;
		fireball.Initialize(caster, direction, Speed, Damage, LifetimeSeconds);

		return true;
	}
}


