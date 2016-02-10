using UnityEngine;
using System.Collections;
	
public class ProjectilePropertySource : IEffectPropertySource
{
	private Projectile target;
	
	public ProjectilePropertySource(Projectile target)
	{
		this.target = target;
	}
	
	public object GetObject(string name)
	{
		switch (name)
		{
		case "gameObject":
			return (target == null) ? null : target.gameObject;
		case "position":
			return (target == null) ? Vector3.zero : target.transform.position;
		case "layer":
			return (target == null) ? 0 : target.gameObject.layer;
		case "velocity":
			return target.Velocity;
		}
		
		return null;
	}
}

public class CollisionPropertySource : IEffectPropertySource
{
	private ShapeRaycastHit hit;
	
	public CollisionPropertySource(ShapeRaycastHit hit)
	{
		this.hit = hit;
	}
	
	public object GetObject(string name)
	{
		switch (name)
		{
		case "gameObject":
			return hit.Shape.ConnectedTo;
		case "position":
			return hit.Position;
		case "normal":
			return hit.Normal;
		}
		
		return null;
	}
}

public class Projectile : EffectGameObject, IFixedUpdate, ITimeTravelable {

	private Vector3 velocity = Vector3.zero;
	private UpdateManager updateManager;
	private TimeManager timeManager;
	private CustomCharacterController characterController;

	private float radius = 0.25f;
	private float bounceFactor = 1.0f;

	private bool useGravity = false;
	
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		velocity = instance.GetValue<Vector3>("direction").normalized * instance.GetValue<float>("speed");
		radius = instance.GetValue<float>("radius", radius);
		bounceFactor = instance.GetValue<float>("bounceFactor", bounceFactor);

		characterController = gameObject.GetOrAddComponent<CustomCharacterController>();
		characterController.offset = Vector2.zero;
		characterController.radius = radius;
		characterController.innerHeight = 0.0f;
		characterController.collisionLayers = instance.GetValue<int>("collideWith", ~0);
		characterController.moveCollisionLayers = instance.GetValue<int>("moveCollideWith", characterController.collisionLayers);
		characterController.AddToIndex(instance.GetContextValue<SpacialIndex>("spacialIndex", null));

		useGravity = instance.GetValue<bool>("useGravity", false);

		updateManager = instance.GetContextValue<UpdateManager>("updateManager", null);
		this.AddToUpdateManager(updateManager);

		timeManager = instance.GetContextValue<TimeManager>("timeManager", null);
		timeManager.AddTimeTraveler(this);
	}

	public override IEffectPropertySource PropertySource
	{
		get
		{
			return new ProjectilePropertySource(this);
		}
	}
	
	public void OnEnable() {
		this.AddToUpdateManager(updateManager);
	}

	public void OnDisable() {
		this.RemoveFromUpdateManager(updateManager);
	}

	public Vector3 Velocity
	{
		get
		{
			return velocity;
		}
	}

	private const float minMoveDist = 0.0001f;
	private const float skinThickness = 0.01f;
	
	void OnCustomControllerHit(ShapeRaycastHit hit)
	{
		instance.TriggerEvent("hit", new CollisionPropertySource(hit));
		
		if (Vector3.Dot(velocity, hit.Normal) < 0)
		{
			velocity = Vector3.Reflect(velocity, hit.Normal) * bounceFactor;
		}
	}

	private void Move(Vector3 amount)
	{
		characterController.Move(new Vector2(amount.x, amount.y));
	}
	
	public void FixedUpdateTick (float dt) {
		velocity.z = 0.0f;
		Move(velocity * dt);

		if (useGravity)
		{
			velocity += Physics.gravity * dt;
		}
	}

	public void OnDrawGizmos()
	{
		Gizmos.DrawSphere(transform.position, radius);
	}
	
	public object GetCurrentState()
	{
		if (gameObject.activeSelf)
		{
			return new object[]{
				TimeGameObject.GetCurrentState(gameObject),
				velocity
			};
		}
		else
		{
			return null;
		}
	}
	
	public void RewindToState(object state)
	{
		if (state == null)
		{
			Destroy(gameObject);
		}
		else
		{
			object[] stateArray = (object[])state;
			TimeGameObject.RewindToState(gameObject, stateArray[0]);
			velocity = (Vector3)stateArray[1];
			characterController.UpdateIndex();
		}
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}
}
