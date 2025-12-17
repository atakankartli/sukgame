using Godot;

public abstract partial class Skill : Resource
{
	[Export] public float Cooldown { get; set; }
	public float CooldownTimer { get; set; } = 0;

	public void UpdateTimer(double delta) => CooldownTimer = Mathf.Max(0, CooldownTimer - (float)delta);
	public bool IsReady => CooldownTimer <= 0;

	public abstract bool Execute(Player player, Vector2 direction);
}
