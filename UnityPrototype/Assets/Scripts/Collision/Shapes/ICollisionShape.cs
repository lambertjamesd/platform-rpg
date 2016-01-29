using UnityEngine;

public interface ICollisionShape
{
	BoundingBox GetBoundingBox();

	SimpleRaycastHit Raycast(Ray2D ray);
	SimpleRaycastHit Spherecast(Ray2D ray, float radius);
	SimpleRaycastHit CapsuleCast(Ray2D ray, float radius, float innerHeight);
}