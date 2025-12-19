using Godot;

[GlobalClass]
public partial class SkillDash : Skill
{
	[Export] public float Speed = 300f;
	[Export] public float Duration = 0.2f;

	public override bool Execute(Node2D caster, Vector2 direction)
	{
		if (direction == Vector2.Zero) return false;
		if (caster is not Player player) return false;
		player.ApplyDash(direction, this.Speed, this.Duration);
		return true;
	}
}
