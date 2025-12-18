using Godot;

public abstract partial class Skill : Resource
{
	[Export] public float Cooldown { get; set; }
	public float CooldownTimer { get; set; } = 0;

	public void UpdateTimer(double delta) => CooldownTimer = Mathf.Max(0, CooldownTimer - (float)delta);
	public bool IsReady => CooldownTimer <= 0;

	// Generalized: any Node2D can be a caster (Player, Enemy, etc.)
	public abstract bool Execute(Node2D caster, Vector2 direction);
}
