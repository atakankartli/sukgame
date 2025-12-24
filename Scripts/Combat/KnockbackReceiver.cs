using Godot;

namespace Combat;

/// <summary>
/// Component that handles knockback for CharacterBody2D entities.
/// Listens to CombatStats.DamageTaken and applies velocity.
/// </summary>
public partial class KnockbackReceiver : Node
{
	[Signal] public delegate void KnockbackStartedEventHandler(Vector2 velocity);
	[Signal] public delegate void KnockbackEndedEventHandler();
	
	#region Configuration
	[ExportGroup("Knockback Settings")]
	/// <summary>Multiplier for incoming knockback force. 0 = immune to knockback.</summary>
	[Export(PropertyHint.Range, "0,2,0.1")] public float KnockbackMultiplier { get; set; } = 1f;
	
	/// <summary>How quickly knockback velocity decays (higher = faster stop).</summary>
	[Export] public float Friction { get; set; } = 800f;
	
	/// <summary>Minimum velocity before knockback is considered complete.</summary>
	[Export] public float MinVelocity { get; set; } = 10f;
	#endregion

	private CharacterBody2D _body;
	private CombatStats _stats;
	private Vector2 _knockbackVelocity;
	private bool _isKnockedBack;

	public bool IsKnockedBack => _isKnockedBack;
	public Vector2 KnockbackVelocity => _knockbackVelocity;

	public override void _Ready()
	{
		// Find CharacterBody2D parent
		_body = GetParent() as CharacterBody2D;
		if (_body == null)
		{
			Node current = GetParent();
			while (current != null)
			{
				if (current is CharacterBody2D cb)
				{
					_body = cb;
					break;
				}
				current = current.GetParent();
			}
		}
		
		if (_body == null)
		{
			GD.PrintErr($"KnockbackReceiver '{Name}': No CharacterBody2D found in parent hierarchy!");
			return;
		}
		
		// Find CombatStats sibling or in parent
		_stats = GetParent().GetNodeOrNull<CombatStats>("CombatStats");
		if (_stats == null)
		{
			_stats = GetNodeOrNull<CombatStats>("../CombatStats");
		}
		
		if (_stats != null)
		{
			// Subscribe to the C# event
			_stats.DamageTaken += OnDamageTaken;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_isKnockedBack) return;
		if (_body == null) return;
		
		float dt = (float)delta;
		
		// Apply friction to slow down
		float speed = _knockbackVelocity.Length();
		if (speed > MinVelocity)
		{
			float newSpeed = Mathf.Max(0, speed - Friction * dt);
			_knockbackVelocity = _knockbackVelocity.Normalized() * newSpeed;
			
			// Apply to body
			_body.Velocity = _knockbackVelocity;
			_body.MoveAndSlide();
		}
		else
		{
			// Knockback finished
			_knockbackVelocity = Vector2.Zero;
			_isKnockedBack = false;
			EmitSignal(SignalName.KnockbackEnded);
		}
	}

	private void OnDamageTaken(DamageInfo info)
	{
		if (info.KnockbackForce <= 0) return;
		if (KnockbackMultiplier <= 0) return;
		
		ApplyKnockback(info.Direction, info.KnockbackForce);
	}

	/// <summary>
	/// Manually apply knockback (can be called directly for non-damage knockback).
	/// </summary>
	public void ApplyKnockback(Vector2 direction, float force)
	{
		if (_body == null) return;
		if (KnockbackMultiplier <= 0) return;
		
		_knockbackVelocity = direction.Normalized() * force * KnockbackMultiplier;
		_isKnockedBack = true;
		
		EmitSignal(SignalName.KnockbackStarted, _knockbackVelocity);
	}

	/// <summary>
	/// Immediately stop any knockback.
	/// </summary>
	public void CancelKnockback()
	{
		if (!_isKnockedBack) return;
		
		_knockbackVelocity = Vector2.Zero;
		_isKnockedBack = false;
		EmitSignal(SignalName.KnockbackEnded);
	}
}
