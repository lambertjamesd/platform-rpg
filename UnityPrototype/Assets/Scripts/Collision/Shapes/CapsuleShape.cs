using UnityEngine;
using System.Collections;

public class CapsuleShape : ICollisionShape {

	public CapsuleShape(float radius, float innerHeight)
	{
		this.radius = radius;
		this.innerHeight = innerHeight;
		CollisionGroup = -1;
		CollisionLayers = ~0;
	}

	public float radius;
	public float innerHeight;

	public Vector2 Center
	{
		get;
		set;
	}

	public BoundingBox GetBoundingBox()
	{
		Vector2 halfSize = new Vector2(radius, radius + innerHeight * 0.5f);
		return new BoundingBox(Center - halfSize, Center + halfSize);
	}
	
	public SimpleRaycastHit Raycast(Ray2D ray)
	{
		return Spherecast(ray, 0.0f);
	}

	public SimpleRaycastHit Spherecast(Ray2D ray, float radius)
	{
		return CapsuleRaycasting.SpherecastCapsule(ray, radius, Center, this.radius, this.innerHeight);
	}

	public SimpleRaycastHit CapsuleCast(Ray2D ray, float radius, float innerHeight)
	{
		return CapsuleRaycasting.CapsulecastCapsule(ray, radius, innerHeight, Center, this.radius, this.innerHeight);
	}
	
	public SimpleOverlap Overlap(ICollisionShape other)
	{
		return SimpleOverlap.Reverse(other.OverlapCapsule(this));
	}
	
	public SimpleOverlap OverlapCircle(CircleShape circle)
	{
		return SimpleOverlap.Reverse(SphereOverlap.SphereCapsuleOverlap(circle.Center, circle.radius, Center, radius, innerHeight));
	}
	
	public SimpleOverlap OverlapCapsule(CapsuleShape capsule)
	{
		return CapsuleOverlap.CapsuleCapsuleOverlap(this.Center, this.radius, this.innerHeight, capsule.Center, capsule.radius, capsule.innerHeight);
	}

	public SimpleOverlap OverlapBoundingBox(BoundingBox bb)
	{
		return SimpleOverlap.Reverse(BoundingBoxOverlap.BoundingBoxCapsuleOverlap(bb, Center, radius, innerHeight));
	}
	
	public int CollisionGroup { get; set; }
	public int CollisionLayers { get; set; }
	public GameObject ConnectedTo { get; set; }
}
