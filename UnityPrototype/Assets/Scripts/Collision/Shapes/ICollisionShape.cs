using UnityEngine;

public interface ICollisionShape
{
	BoundingBox GetBoundingBox();

	SimpleRaycastHit Raycast(Ray2D ray);
	SimpleRaycastHit Spherecast(Ray2D ray, float radius);
	SimpleRaycastHit CapsuleCast(Ray2D ray, float radius, float innerHeight);

	SimpleOverlap Overlap(ICollisionShape other);

	SimpleOverlap OverlapCircle(CircleShape circle);
	SimpleOverlap OverlapCapsule(CapsuleShape capsule);
	SimpleOverlap OverlapBoundingBox(BoundingBox bb);

	int CollisionGroup { get; }
	int CollisionLayers { get; }
	GameObject ConnectedTo { get; }
}

public static class CollisionShape
{
	public static bool Collides(ICollisionShape shape, int collisionGroup, int collisionLayers)
	{
		return ((shape.CollisionGroup != collisionGroup) || collisionGroup == -1) && (collisionLayers & shape.CollisionLayers) != 0;
	}
}