using UnityEngine;

public static class SphereOverlap
{
	public static SimpleOverlap SphereSphereOverlap(Vector2 a, float radiusA, Vector2 b, float radiusB)
	{
		Vector2 offset = b - a;

		if (offset.sqrMagnitude <= (radiusA + radiusB) * (radiusA + radiusB))
		{
			Vector2 offsetNormalized = offset.normalized;
			return new SimpleOverlap(a + offsetNormalized * radiusA, b - offsetNormalized * radiusB);
		}
		else
		{
			return null;
		}
	}

	public static SimpleOverlap SphereCapsuleOverlap(Vector2 sphere, float sphereRadius, Vector2 capsule, float capsuleRadius, float innerHeight)
	{
		return SphereSphereOverlap(
			sphere, 
			sphereRadius, 
			new Vector2(capsule.x, Mathf.Clamp(sphere.y, capsule.y - innerHeight * 0.5f, capsule.y + innerHeight * 0.5f)), 
			capsuleRadius
		);
	}

	public static SimpleOverlap SpherePointOverlap(Vector2 a, float radius, Vector2 point)
	{
		return SphereSphereOverlap(a, radius, point, 0.0f);
	}

	public static SimpleOverlap SphereBBOverlap(Vector2 a, float radius, BoundingBox box)
	{
		Vector2 clampedCenter = box.NearestPoint(a);

		if (clampedCenter == a)
		{
			float distance = box.max.y - a.y;
			Vector2 from = a - Vector2.up * radius;
			Vector2 to = new Vector2(a.x, box.max.y);

			if (a.y - box.min.y < distance)
			{
				from = a + Vector2.up * radius;
				to = new Vector2(a.x, box.min.y);
			}

			if (box.max.x - a.x < distance)
			{
				from = a - Vector2.right * radius;
				to = new Vector2(box.max.x, a.y);
			}

			if (a.x - box.min.x < distance)
			{
				from = a + Vector2.right * radius;
				to = new Vector2(box.min.x, a.y);
			}

			return new SimpleOverlap(from, to);
		}
		else
		{
			return SpherePointOverlap(a, radius, clampedCenter);
		}
	}
}
