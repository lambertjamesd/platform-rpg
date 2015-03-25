using UnityEngine;
using System.Collections;

public static class GizmoHelper {

	// i'ts over 9000!
	public static float reallyLong = 10000.0f;

	public static void DrawSphereCast(Vector3 position, Vector3 direction, float radius, Color color)
	{
		Color prevColor = Gizmos.color;
		Gizmos.color = color;
		direction.Normalize();
		Vector3 sideOffset = new Vector3(-direction.y, direction.x, 0.0f) * radius;

		Gizmos.DrawLine(position + sideOffset, position + sideOffset + direction * reallyLong);
		Gizmos.DrawLine(position - sideOffset, position - sideOffset + direction * reallyLong);
		Gizmos.DrawWireSphere(position, radius);
		Gizmos.color = prevColor;
	}

	public static void DrawThickLine(Vector3 a, Vector3 b, float radius, Color color)
	{
		Color prevColor = Gizmos.color;
		Gizmos.color = color;
		Vector3 direction = (b - a).normalized * radius;
		Vector3 sideOffset = new Vector3(-direction.y, direction.x, 0.0f);

		Gizmos.DrawLine(a + sideOffset, b + sideOffset);
		Gizmos.DrawLine(a - sideOffset, b - sideOffset);
		Gizmos.DrawWireSphere(a, radius);
		Gizmos.DrawWireSphere(b, radius);
		Gizmos.color = prevColor;
	}

	public static void DrawCapsule(Vector3 center, Vector3 up, float height, float radius, Color color)
	{
		float halfOffset = height * 0.5f - radius;
		DrawThickLine(center + up * halfOffset, center - up * halfOffset, radius, color);
	}
}
