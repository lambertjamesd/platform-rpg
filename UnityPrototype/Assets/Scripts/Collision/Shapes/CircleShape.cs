using UnityEngine;

public class CircleShape : ICollisionShape
{
	public CircleShape(float radius)
	{
		this.radius = radius;
		CollisionGroup = -1;
		CollisionLayers = ~0;
	}

	public Vector2 offset;
	public float radius;

	public Vector2 Center
	{
		get;
		set;
	}

	public BoundingBox GetBoundingBox()
	{
		Vector2 halfSize = new Vector2(radius, radius);
		return new BoundingBox(Center- halfSize, Center + halfSize);
	}
	
	public SimpleRaycastHit Raycast(Ray2D ray)
	{
		SimpleRaycastHit hit = Raycasting.SpherecastPoint(new Ray2D(Center, -ray.direction), radius, ray.origin);

		if (hit == null)
		{
			return null;
		}
		else
		{
			return new SimpleRaycastHit(Center - hit.Normal * radius, -hit.Normal, hit.Distance);
		}
	}

	public SimpleRaycastHit Spherecast(Ray2D ray, float radius)
	{
		SimpleRaycastHit hit = Raycasting.SpherecastPoint(new Ray2D(Center, -ray.direction), radius + this.radius, ray.origin);
		
		if (hit == null)
		{
			return null;
		}
		else
		{
			return new SimpleRaycastHit(Center - hit.Normal * this.radius, -hit.Normal, hit.Distance);
		}
	}

	public SimpleRaycastHit CapsuleCast(Ray2D ray, float radius, float innerHeight)
	{
		SimpleRaycastHit hit = CapsuleRaycasting.SpherecastCapsule(new Ray2D(Center, -ray.direction), this.radius, ray.origin, radius, innerHeight);

		if (hit == null)
		{
			return null;
		}
		else
		{
			return new SimpleRaycastHit(Center - hit.Normal * radius, -hit.Normal, hit.Distance);
		}
	}
	
	
	public SimpleOverlap Overlap(ICollisionShape other)
	{
		return SimpleOverlap.Reverse(other.OverlapCircle(this));
	}
	
	public SimpleOverlap OverlapCircle(CircleShape circle)
	{
		return SphereOverlap.SphereSphereOverlap(Center, radius, circle.Center, circle.radius);
	}
	
	public SimpleOverlap OverlapCapsule(CapsuleShape capsule)
	{
		return SphereOverlap.SphereCapsuleOverlap(Center, radius, capsule.Center, capsule.radius, capsule.innerHeight);
	}

	public SimpleOverlap OverlapBoundingBox(BoundingBox bb)
	{
		return SphereOverlap.SphereBBOverlap(Center, radius, bb);
	}
	
	public int CollisionGroup { get; set; }
	public int CollisionLayers { get; set; }
	public GameObject ConnectedTo { get; set; }
}
