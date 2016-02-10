using UnityEngine;
using System.Collections;

public class CustomCapsule : CustomCollider {

	public Vector2 offset;
	public float radius;
	public float innerHeight;

	protected CapsuleShape shape;

	public override void InitializeShape()
	{
		shape = new CapsuleShape(radius, innerHeight);
		shape.CollisionGroup = collisionGroup;
		shape.CollisionLayers = collisionLayers;
		shape.ConnectedTo = gameObject;
	}

	public override ICollisionShape GetShape()
	{
		return shape;
	}

	public override void UpdateProperties()
	{
		if (shape != null)
		{
			shape.radius = radius;
			shape.innerHeight = innerHeight;
			shape.CollisionGroup = collisionGroup;
			shape.CollisionLayers = collisionLayers;
		}
	}

	public override void UpdateIndex()
	{
		Vector2 center = transform.position;
		shape.Center = center + offset;
		base.UpdateIndex();
	}

	public void OnDrawGizmos()
	{
		if (shape != null)
		{
			Vector2 a = shape.Center + new Vector2(0.0f, innerHeight * 0.5f);
			Vector2 b = shape.Center - new Vector2(0.0f, innerHeight * 0.5f);

			Gizmos.DrawWireSphere(new Vector3(a.x, a.y), radius);
			Gizmos.DrawWireSphere(new Vector3(b.x, b.y), radius);
		}
	}
}
