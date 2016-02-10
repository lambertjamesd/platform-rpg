using UnityEngine;

public static class CapsuleOverlap
{
	public static SimpleOverlap CapsuleCapsuleOverlap(Vector2 a, float aRadius, float aInnerHeight, Vector2 b, float bRadius, float bInnerHeight)
	{
		float upperA = a.y + aInnerHeight * 0.5f;
		float lowerA = a.y - aInnerHeight * 0.5f;

		float upperB = b.y + bInnerHeight * 0.5f;
		float lowerB = b.y - bInnerHeight * 0.5f;

		float commonPoint = (Mathf.Max(lowerA, lowerB) + Mathf.Min(upperA, upperB)) * 0.5f;
		return SphereOverlap.SphereSphereOverlap(
			new Vector2(a.x, Mathf.Clamp(commonPoint, lowerA, upperA)),
			aRadius,
			new Vector2(b.x, Mathf.Clamp(commonPoint, lowerB, upperB)),
			bRadius
		);
	}
}