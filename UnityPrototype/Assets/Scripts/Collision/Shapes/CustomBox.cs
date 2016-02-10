using UnityEngine;

public class CustomBox : CustomCollider
{
	public Vector2 offset;
	public Vector2 size;

	protected BoundingBoxShape shape;

	private BoundingBox GetBoundingBox()
	{
		Vector2 pos = transform.position;
		pos += offset;
		return new BoundingBox(pos - size * 0.5f, pos + size * 0.5f);
	}

	public override void InitializeShape()
	{
		shape = new BoundingBoxShape(GetBoundingBox());
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
			shape.SetBoundingBox(GetBoundingBox());
			shape.CollisionGroup = collisionGroup;
			shape.CollisionLayers = collisionLayers;
		}
	}

	public override void UpdateIndex()
	{
		shape.SetBoundingBox(GetBoundingBox());
		base.UpdateIndex();
	}

	public void OnDrawGizmos()
	{
		if (shape != null)
		{
			Vector3 offset3 = new Vector3(offset.x, offset.y);
			Vector3 size3 = new Vector3(size.x, size.y);
			Gizmos.DrawWireCube(transform.position + offset3, size3);
		}
	}
}
