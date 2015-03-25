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
	private ControllerColliderHit hit;
	
	public CollisionPropertySource(ControllerColliderHit hit)
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

public class Projectile : EffectGameObject, IFixedUpdate {

	private Vector3 velocity = Vector3.zero;
	private CharacterController characterController;
	private UpdateManager updateManager;

	private float radius = 0.25f;
	private float bounceFactor = 1.0f;

	private bool useGravity = false;
	
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		velocity = instance.GetValue<Vector3>("direction").normalized * instance.GetValue<float>("speed");
		radius = instance.GetValue<float>("radius", radius);
		bounceFactor = instance.GetValue<float>("bounceFactor", bounceFactor);

		characterController = gameObject.GetOrAddComponent<CharacterController>();
		characterController.height = radius * 2.0f;
		characterController.radius = radius;

		useGravity = instance.GetValue<bool>("useGravity", false);

		updateManager = instance.GetContextValue<UpdateManager>("updateManager", null);
		this.AddToUpdateManager(updateManager);
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
	
	public void FixedUpdateTick (float dt) {
		velocity.z = 0.0f;
		characterController.Move(velocity * dt);

		if (useGravity)
		{
			velocity += Physics.gravity * dt;
		}
	}

	public void OnDrawGizmos()
	{
		Gizmos.DrawSphere(transform.position, radius);
	}

	public void OnControllerColliderHit(ControllerColliderHit hit)
	{
		instance.TriggerEvent("hit", new CollisionPropertySource(hit));

		if (Vector3.Dot(velocity, hit.normal) < 0)
		{
			velocity = Vector3.Reflect(velocity, hit.normal) * bounceFactor;
		}
	}
}
