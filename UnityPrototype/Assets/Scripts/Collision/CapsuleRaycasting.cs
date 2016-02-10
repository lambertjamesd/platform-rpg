using UnityEngine;

public static class CapsuleRaycasting
{
	public static SimpleRaycastHit SpherecastCapsule(Ray2D ray, float castRadius, Vector2 center, float capsuleRadius, float innerHeight)
	{
		Vector2 halfOffset = new Vector2(0.0f, innerHeight * 0.5f);
		Vector2 a = center + halfOffset;
		Vector2 b = center - halfOffset;
		
		Vector2 sideNormal = new Vector2(-Mathf.Sign(ray.direction.x), 0.0f);
		
		float totalRadius = capsuleRadius + castRadius;
		
		SimpleRaycastHit hit = Raycasting.SpherecastLineSegment(ray, totalRadius, a, b, sideNormal) ??
			SimpleRaycastHit.NearestHit(
				Raycasting.SpherecastPoint(ray, totalRadius, a),
				Raycasting.SpherecastPoint(ray, totalRadius, b)
				);
		
		if (hit != null)
		{
			return new SimpleRaycastHit(hit.Position + hit.Normal * capsuleRadius, hit.Normal, hit.Distance);
		}
		else
		{
			return null;
		}
	}

	public static SimpleRaycastHit CapsulecastCapsule(Ray2D ray, float castRadius, float castInnerHeight, Vector2 center, float capsuleRadius, float innerHeight)
	{
		Vector2 castCenter;
		
		if (ray.direction.x == 0.0f)
		{
			castCenter = ray.origin + new Vector2(0.0f, castInnerHeight * 0.5f * Mathf.Sign(ray.direction.y));
		}
		else
		{
			float totalRadius = capsuleRadius + castRadius;
			float yCastPos = center.y + (ray.origin.x + totalRadius * Mathf.Sign(ray.direction.x) - center.x) * ray.direction.y / ray.direction.x;
			float clampedPos = Mathf.Clamp(yCastPos, ray.origin.y - castInnerHeight * 0.5f, ray.origin.y + castInnerHeight * 0.5f);
			castCenter = new Vector2(ray.origin.x, clampedPos);
		}
		
		return SpherecastCapsule(new Ray2D(castCenter, ray.direction), castRadius, center, capsuleRadius, innerHeight);
	}
}
