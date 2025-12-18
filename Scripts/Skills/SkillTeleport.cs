using Godot;

[GlobalClass]
public partial class SkillTeleport : Skill
{
	[Export] public float Distance = 200f;
	// How long to wait before the player actually moves to the target position.
	// This is saved into the .tres (e.g. Teleport.tres) so you can tune it in the Inspector.
	[Export] public float DelaySeconds = 0.5f;
	[Export] public float Duration = 1.0f;

	public override bool Execute(Node2D caster, Vector2 direction)
	{
		if (direction == Vector2.Zero) return false;
		if (caster is not Player player) return false;
		player.ApplyTeleport(direction, this.Distance, this.DelaySeconds, this.Duration);
		CooldownTimer = Cooldown;
		return true;
	}
}
