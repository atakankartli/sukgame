using Godot;

public enum SkillAimSource
{
	MoveInput = 0,
	Mouse = 1,
}

public abstract partial class Skill : Resource
{
	// How the player should aim this skill.
	// (Enemy AI can still pass whatever direction it wants to Execute.)
	[Export] public SkillAimSource AimSource { get; set; } = SkillAimSource.MoveInput;
	[Export] public float Cooldown { get; set; }

	// Generalized: any Node2D can be a caster (Player, Enemy, etc.)
	public abstract bool Execute(Node2D caster, Vector2 direction);
}
