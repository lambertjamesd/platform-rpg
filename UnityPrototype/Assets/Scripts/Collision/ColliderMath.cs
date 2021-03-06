using System;
using UnityEngine;

public static class ColliderMath
{
	public static readonly float ZERO_TOLERANCE =  0.00001f;

	public static Vector2 Intersection(float radius, Vector2 normal1, Vector2 normal2)
	{
		float normalCross = Cross2D(normal1, normal2);

		if (Mathf.Abs(normalCross) < ZERO_TOLERANCE)
		{
			return new Vector2(float.NaN, float.NaN);
		}
		else
		{
			float x = radius * (normal2.y - normal1.y) / normalCross;

			if (Mathf.Abs(normal1.y) > Mathf.Abs(normal2.y))
			{
				return new Vector2(x, (radius - x * normal1.x) / normal1.y);
			}
			else
			{
				return new Vector2(x, (radius - x * normal2.x) / normal2.y);
			}
		}
	}

	public static float Cross2D(Vector2 a, Vector2 b)
	{
		return a.x * b.y - a.y * b.x;
	}

	public static Vector2 RotateCW(Vector2 input)
	{
		return new Vector2(input.y, -input.x);
	}
	
	public static Vector2 RotateCCW(Vector2 input)
	{
		return new Vector2(-input.y, input.x);
	}

	public static bool InFrontOf(Vector2 point, Vector2 planeNormal, Vector2 planePoint)
	{
		return Vector2.Dot(planeNormal, point - planePoint) > 0.0f;
	}

	public static bool DoesCollide(Ray ray, Vector3 sphereCenter, float sphereRadius)
	{
		Vector3 pointToCheck = ray.ProjectOnto(sphereCenter);

		return (pointToCheck - sphereCenter).sqrMagnitude <= sphereRadius * sphereRadius;
	}

	public static Vector3 ProjectOnto(this Ray ray, Vector3 toProject)
	{
		return ray.origin + Vector3.Project(toProject - ray.origin, ray.direction);
	}

	public static Ray TransformRay(Ray input, Transform transform)
	{
		return new Ray(transform.TransformPoint(input.origin), transform.TransformDirection(input.direction));
	}
	
	public static Ray InverseTransformRay(Ray input, Transform transform)
	{
		return new Ray(transform.InverseTransformPoint(input.origin), transform.InverseTransformDirection(input.direction));
	}
}

