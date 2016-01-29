using System;
using UnityEngine;

public static class Vector2Helper
{
	public static Vector2 Rotate90(Vector2 input)
	{
		return new Vector2(-input.y, input.x);
	}

	public static float DistanceAlongRay(Ray2D ray, Vector2 point)
	{
		return Vector2.Dot(ray.direction, point - ray.origin);
	}

	public static Vector2 RotateWithVector(Vector2 input, Vector2 unitVector)
	{
		return new Vector2(input.x * unitVector.x - input.y * unitVector.y,
		                   input.x * unitVector.y + input.y * unitVector.x);
	}

	public static bool NearlyEquals(Vector2 a, Vector2 b)
	{
		return Math.Abs(a.x - b.x) < Raycasting.ERROR_TOLERANCE && Math.Abs(a.y - b.y) < Raycasting.ERROR_TOLERANCE;
	}

	public static Vector2 OrdinalDirection(Vector2 input)
	{
		return Mathf.Abs(input.x) >= Mathf.Abs(input.y) ?
			new Vector2(Mathf.Sign(input.x), 0.0f) :
			new Vector2(0.0f, Mathf.Sign(input.y));
	}
}

