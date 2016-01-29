using UnityEngine;

public class BoundingBoxShape : ICollisionShape
{
	private BoundingBox boundingBox;

	public BoundingBoxShape(BoundingBox box)
	{
		this.boundingBox = box;
	}

	public BoundingBox GetBoundingBox()
	{
		return boundingBox;
	}
	
	public SimpleRaycastHit Raycast(Ray2D ray)
	{
		return Raycasting.RaycastBoundingBox(ray, boundingBox);
	}

	public SimpleRaycastHit Spherecast(Ray2D ray, float radius)
	{
		return Raycasting.SpherecastBoundingBox(ray, radius, boundingBox);
	}

	public SimpleRaycastHit CapsuleCast(Ray2D ray, float radius, float innerHeight)
	{
		return Raycasting.CapsulecastBoundingBox(ray, radius, innerHeight, boundingBox);
	}
}
