using Godot;
using Combat;

public partial class Fireball : Area2D
{
	private Vector2 _direction = Vector2.Zero;
	private float _speed;
	private float _damage;
	private float _lifeSeconds;
	private float _age;
	private Node2D _owner;

	public void Initialize(Node2D owner, Vector2 direction, float speed, float damage, float lifetimeSeconds)
	{
		_owner = owner;
		_direction = direction.Normalized();
		_speed = speed;
		_damage = damage;
		_lifeSeconds = lifetimeSeconds;

		// Point the fireball in the movement direction
		if (_direction != Vector2.Zero)
			Rotation = _direction.Angle();
	}

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		AreaEntered += OnAreaEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_direction == Vector2.Zero)
		{
			QueueFree();
			return;
		}

		GlobalPosition += _direction * _speed * (float)delta;

		_age += (float)delta;
		if (_age >= _lifeSeconds)
			QueueFree();
	}

	private void OnBodyEntered(Node body)
	{
		// Don't hit the spawner
		if (_owner != null && body == _owner)
			return;

		// Try new damage system first (IDamageable)
		if (body is IDamageable damageable)
		{
			var damageInfo = DamageInfo.Elemental(_damage, DamageType.Fire, _owner, _direction, 80f);
			damageable.TakeDamage(damageInfo);
			QueueFree();
			return;
		}

		// Fallback: Legacy damage method for backwards compatibility
		if (body.HasMethod("Damage"))
		{
			body.Call("Damage", _damage);
		}

		QueueFree();
	}

	private void OnAreaEntered(Area2D area)
	{
		// Check for Hurtbox (new system)
		if (area is Hurtbox hurtbox)
		{
			var ownerEntity = hurtbox.GetOwnerEntity();
			
			// Don't hit the spawner
			if (_owner != null && ownerEntity == _owner)
				return;

			var damageInfo = DamageInfo.Elemental(_damage, DamageType.Fire, _owner, _direction, 80f);
			hurtbox.ReceiveDamage(damageInfo);
			QueueFree();
		}
	}
}
