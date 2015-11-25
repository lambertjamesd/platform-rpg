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
	private RaycastHit hit;
	
	public CollisionPropertySource(RaycastHit hit)
	{
		this.hit = hit;
	}
	
	public object GetObject(string name)
	{
		switch (name)
		{
		case "gameObject":
			return hit.collider.gameObject;
		case "position":
			return hit.point;
		case "normal":
			return hit.normal;
		}
		
		return null;
	}
}

public class Projectile : EffectGameObject, IFixedUpdate, ITimeTravelable {

	private Vector3 velocity = Vector3.zero;
	private SphereCollider sphereCollider;
	private UpdateManager updateManager;
	private TimeManager timeManager;

	private float radius = 0.25f;
	private float bounceFactor = 1.0f;

	private bool useGravity = false;

	private int collideWith;
	
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		velocity = instance.GetValue<Vector3>("direction").normalized * instance.GetValue<float>("speed");
		radius = instance.GetValue<float>("radius", radius);
		bounceFactor = instance.GetValue<float>("bounceFactor", bounceFactor);

		sphereCollider = gameObject.GetOrAddComponent<SphereCollider>();
		sphereCollider.radius = radius;

		useGravity = instance.GetValue<bool>("useGravity", false);

		collideWith = instance.GetValue<int>("collideWith", ~0);

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

	private void Move(Vector3 amount)
	{
		while (amount.sqrMagnitude > minMoveDist * minMoveDist)
		{
			Vector3 direction = amount.normalized;

			RaycastHit hitInfo;

			if (Physics.SphereCast(transform.position, radius - skinThickness, direction, out hitInfo, Vector3.Dot(direction, amount), collideWith))
			{
				transform.position += direction * (hitInfo.distance - skinThickness / Vector3.Dot(hitInfo.normal, -direction));
				
				instance.TriggerEvent("hit", new CollisionPropertySource(hitInfo));
				
				if (Vector3.Dot(velocity, hitInfo.normal) < 0)
				{
					amount -= Vector3.Project(amount, hitInfo.normal);
					velocity = Vector3.Reflect(velocity, hitInfo.normal) * bounceFactor;
				}
			}
			else
			{
				transform.position += amount;
				amount = Vector3.zero;
			}
		}
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
		}
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}
}
