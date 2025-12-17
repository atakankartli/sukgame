using Godot;

[GlobalClass]
public partial class SkillTeleport : Skill
{
	[Export] public float Distance = 200f;
	[Export] public float Duration = 1.0f;

	public override bool Execute(Player player, Vector2 direction)
	{
		if (direction == Vector2.Zero) return false;
		player.ApplyTeleport(direction, this.Distance, this.Duration);
		CooldownTimer = Cooldown;
		return true;
	}
}
