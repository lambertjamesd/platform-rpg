using UnityEngine;

public class BoundingBoxShape : ICollisionShape
{
	private BoundingBox boundingBox;

	public BoundingBoxShape(BoundingBox box)
	{
		this.boundingBox = box;
		CollisionGroup = -1;
		CollisionLayers = ~0;
	}

	public BoundingBox GetBoundingBox()
	{
		return boundingBox;
	}

	public void SetBoundingBox(BoundingBox value)
	{
		boundingBox = value;
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
	
	public SimpleOverlap Overlap(ICollisionShape other)
	{
		return SimpleOverlap.Reverse(other.OverlapBoundingBox(this.boundingBox));
	}
	
	public SimpleOverlap OverlapCircle(CircleShape circle)
	{
		return SimpleOverlap.Reverse(SphereOverlap.SphereBBOverlap(circle.Center, circle.radius, boundingBox));
	}
	
	public SimpleOverlap OverlapCapsule(CapsuleShape capsule)
	{
		return BoundingBoxOverlap.BoundingBoxCapsuleOverlap(boundingBox, capsule.Center, capsule.radius, capsule.innerHeight);
	}

	public SimpleOverlap OverlapBoundingBox(BoundingBox bb)
	{
		return BoundingBoxOverlap.BoundingBoxBoundingBoxOverlap(boundingBox, bb);
	}
	
	public int CollisionGroup { get; set; }
	public int CollisionLayers { get; set; }
	public GameObject ConnectedTo { get; set; }
}
