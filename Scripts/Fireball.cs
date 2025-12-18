using Godot;

public partial class Fireball : Area2D
{
	private Vector2 _direction = Vector2.Zero;
	private float _speed;
	private float _damage;
	private float _lifeSeconds;
	private float _age;
	private Node _owner;

	public void Initialize(Node owner, Vector2 direction, float speed, float damage, float lifetimeSeconds)
	{
		_owner = owner;
		_direction = direction.Normalized();
		_speed = speed;
		_damage = damage;
		_lifeSeconds = lifetimeSeconds;

		// Point the fireball in the movement direction (optional)
		if (_direction != Vector2.Zero)
			Rotation = _direction.Angle();
	}

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
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

		// Generic damage call: any node with a Damage(float) method can be hit.
		if (body.HasMethod("Damage"))
		{
			body.Call("Damage", _damage);
		}

		QueueFree();
	}
}
