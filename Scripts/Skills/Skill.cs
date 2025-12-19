using Godot;

public abstract partial class Skill : Resource
{
	[Export] public float Cooldown { get; set; }

	// Generalized: any Node2D can be a caster (Player, Enemy, etc.)
	public abstract bool Execute(Node2D caster, Vector2 direction);
}
