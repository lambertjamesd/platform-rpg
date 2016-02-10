using UnityEngine;

public class CustomCircle : CustomCollider
{
	public Vector2 offset;
	public float radius;
	
	protected CircleShape shape;
	
	public override void InitializeShape()
	{
		shape = new CircleShape(radius);
		shape.CollisionGroup = collisionGroup;
		shape.CollisionLayers = collisionLayers;
		shape.ConnectedTo = gameObject;
	}
	
	public void Awake()
	{
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
			Vector2 center = transform.position;
			center += offset;
			Gizmos.DrawWireSphere(new Vector3(center.x, center.y), radius);
		}
	}
}
