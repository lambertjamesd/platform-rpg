using System;
using System.Collections;
using UnityEngine;

public class SimpleRaycastHit
{
	private Vector2 position;
	private Vector2 normal;
	private float distance;

	public SimpleRaycastHit(Vector2 position, Vector2 normal, float distance)
	{
		this.position = position;
		this.normal = normal;
		this.distance = distance;
	}

	public Vector2 Position
	{
		get
		{
			return position;
		}
	}

	public Vector2 Normal
	{
		get
		{
			return normal;
		}
	}

	public float Distance
	{
		get
		{
			return distance;
		}
	}
	
	public static SimpleRaycastHit NearestHit(SimpleRaycastHit a, SimpleRaycastHit b)
	{
		if (a == null)
		{
			return b;
		}
		else if (b == null)
		{
			return a;
		}
		else
		{
			if (a.Distance <= b.Distance)
			{
				return a;
			}
			else
			{
				return b;
			}
		}
	}

	public static T FilterByNormal<T>(T input, Vector2 normal) where T : SimpleRaycastHit
	{
		if (input != null && Vector2.Dot(input.Normal, normal) >= -Raycasting.DOT_ERROR_TOLERANCE)
		{
			return input;
		}

		return null;
	}
}

public class ShapeRaycastHit : SimpleRaycastHit
{
	private ICollisionShape shape;

	public ShapeRaycastHit(ICollisionShape shape, SimpleRaycastHit hit) : base(hit.Position, hit.Normal, hit.Distance)
	{
		this.shape = shape;
	}

	public ShapeRaycastHit(ICollisionShape shape, Vector2 position, Vector2 normal, float distance) : base(position, normal, distance)
	{
		this.shape = shape;
	}

	public ICollisionShape Shape
	{
		get
		{
			return shape;
		}
	}
	
	public static ShapeRaycastHit NearestHit(ShapeRaycastHit a, ShapeRaycastHit b)
	{
		if (a == null)
		{
			return b;
		}
		else if (b == null)
		{
			return a;
		}
		else
		{
			if (a.Distance < b.Distance)
			{
				return a;
			}
			else if (b.Distance < a.Distance)
			{
				return b;
			}
			else
			{
				// for determinism
				if (a.shape.GetHashCode() < b.shape.GetHashCode())
				{
					return a;
				}
				else
				{
					return b;
				}
			}
		}
	}
}

public static class Raycasting
{
	public static readonly float ERROR_TOLERANCE = 0.00001f;
	public static readonly float DOT_ERROR_TOLERANCE = 0.0001f;

	public static SimpleRaycastHit RaycastLineSegment(Ray2D ray, Vector2 a, Vector2 b, Vector2 normal)
	{
		float cosAngle = Vector2.Dot(ray.direction, normal);

		if (cosAngle < 0.0f)
		{
			float distance = Vector3.Dot(a - ray.origin, normal) / cosAngle;

			if (distance >= 0.0f)
			{
				Vector2 contactPoint = ray.GetPoint(distance);

				Vector2 segmentOffset = b - a;
				float segmentPos = (Mathf.Abs(segmentOffset.x) > Mathf.Abs(segmentOffset.y)) ?
					(contactPoint.x - a.x) / segmentOffset.x :
					(contactPoint.y - a.y) / segmentOffset.y;

				if (segmentPos >= -ERROR_TOLERANCE && segmentPos <= 1.0f + ERROR_TOLERANCE)
				{
					return new SimpleRaycastHit(contactPoint, normal, distance);
				}
			}
		}

		return null;
	}

	public static SimpleRaycastHit SpherecastLineSegment(Ray2D ray, float radius, Vector2 a, Vector2 b, Vector2 normal)
	{
		Vector2 offset = normal * radius;

		SimpleRaycastHit result = RaycastLineSegment(ray, a + offset, b + offset, normal);

		if (result != null)
		{
			result = new SimpleRaycastHit(result.Position - offset, result.Normal, result.Distance);
		}

		return result;
	}

	public static SimpleRaycastHit CapsulecastLineSegment(Ray2D ray, float radius, float innerHeight, Vector2 a, Vector2 b, Vector2 normal)
	{
		Vector2 castOrigin;

		if (normal.y != 0.0f)
		{
			castOrigin = ray.origin + ((normal.y > 0.0f) ? -Vector2.up * innerHeight * 0.5f : Vector2.up * innerHeight * 0.5f);
		}
		else
		{
			if (ray.direction.x * normal.x >= 0.0f)
			{
				return null;
			}
			else
			{
				float distance = ((a.x - Mathf.Sign(ray.direction.x) * radius) - ray.origin.x) / ray.direction.x;
				float yPos = ray.origin.y + distance * ray.direction.y;
				float minY = Mathf.Min(a.y, b.y);
				float maxY = Mathf.Max(a.y, b.y);

				float actualY = Mathf.Clamp(yPos, minY,  maxY);

				if (Mathf.Abs(actualY - yPos) < innerHeight * 0.5f && distance >= 0.0f)
				{
					return new SimpleRaycastHit(new Vector2(a.x, actualY), normal, distance);
				}
				else
				{
					return null;
				}
			}
		}

		return SpherecastLineSegment(new Ray2D(castOrigin, ray.direction), radius, a, b, normal);
	}

	public static SimpleRaycastHit SpherecastPoint(Ray2D ray, float radius, Vector2 point)
	{
		Vector2 crossDirection = Vector2Helper.Rotate90(ray.direction);
		float crossDistance = Vector2.Dot(point - ray.origin, crossDirection);

		if (crossDistance <= radius)
		{
			float distance = Vector2Helper.DistanceAlongRay(ray, point) - Mathf.Sqrt(radius * radius - crossDistance * crossDistance);

			if (distance >= 0.0)
			{
				return new SimpleRaycastHit(point, (ray.GetPoint(distance) - point) * (1.0f / radius), distance);
			}
		}

		return null;
	}

	public static SimpleRaycastHit CapsulecastPoint(Ray2D ray, float radius, float innerHeight, Vector2 point)
	{
		Vector2 sideNormal = new Vector2(Mathf.Sign(ray.direction.x), 0.0f);

		if (sideNormal.x == 0.0f)
		{
			return SpherecastPoint(
				new Ray2D(ray.origin + Vector2.up * innerHeight * 0.5f * Mathf.Sign(ray.direction.y), ray.direction),
				radius,
				point
			);
		}
		else
		{
			Vector2 centerA = point + Vector2.up * innerHeight * 0.5f;
			Vector2 centerB = point - Vector2.up * innerHeight * 0.5f;

			SimpleRaycastHit hit = SpherecastLineSegment(ray, radius, centerA, centerB, -sideNormal) ?? 
				SpherecastPoint(ray, radius, centerA) ?? 
				SpherecastPoint(ray, radius, centerB);

			if (hit != null)
			{
				return new SimpleRaycastHit(point, hit.Normal, hit.Distance);
			}

			return null;
		}
	}

	public static SimpleRaycastHit RaycastBoundingBox(Ray2D ray, BoundingBox bb)
	{
		if (ray.direction.x == 0.0f || ray.direction.y == 0.0f)
		{
			return RaycastLineSegment(
				ray, 
				bb.CornerInDirection(Vector2Helper.RotateWithVector(ray.direction, new Vector2(-1, 1))),
				bb.CornerInDirection(Vector2Helper.RotateWithVector(ray.direction, new Vector2(-1, -1))),
				new Vector2(Mathf.Sign(ray.direction.x), Mathf.Sign(ray.direction.y))
			);
		}
		else
		{
			Vector2 corner = bb.CornerInDirection(-ray.direction);
			Vector2 sideA = bb.CornerInDirection(Vector2Helper.Rotate90(ray.direction));
			Vector2 sideB = bb.CornerInDirection(-Vector2Helper.Rotate90(ray.direction));

			return RaycastLineSegment(ray, sideA, corner, Vector2Helper.OrdinalDirection(corner - sideB)) ?? 
				RaycastLineSegment(ray, sideB, corner, Vector2Helper.OrdinalDirection(corner - sideA));
		}
	}

	public static SimpleRaycastHit SpherecastBoundingBox(Ray2D ray, float radius, BoundingBox bb)
	{
		if (ray.direction.x == 0.0f || ray.direction.y == 0.0f)
		{
			Vector2 a = bb.CornerInDirection(Vector2Helper.RotateWithVector(ray.direction, new Vector2(-1, 1)));
			Vector2 b = bb.CornerInDirection(Vector2Helper.RotateWithVector(ray.direction, new Vector2(-1, -1)));
			return SpherecastLineSegment(
				ray, 
				radius,
				a,
				b,
				Vector2Helper.OrdinalDirection(-ray.direction)
				) ??
				SpherecastPoint(ray, radius, a) ??
				SpherecastPoint(ray, radius, b);
		}
		else
		{
			Vector2 corner = bb.CornerInDirection(-ray.direction);
			Vector2 sideA = bb.CornerInDirection(Vector2Helper.Rotate90(ray.direction));
			Vector2 sideB = bb.CornerInDirection(-Vector2Helper.Rotate90(ray.direction));
			
			return SpherecastLineSegment(ray, radius, sideA, corner, Vector2Helper.OrdinalDirection(corner - sideB)) ?? 
				SpherecastLineSegment(ray, radius, sideB, corner, Vector2Helper.OrdinalDirection(corner - sideA)) ??
				SpherecastPoint(ray, radius, corner) ??
				SpherecastPoint(ray, radius, sideA) ??
				SpherecastPoint(ray, radius, sideB);
		}
	}

	public static SimpleRaycastHit CapsulecastBoundingBox(Ray2D ray, float radius, float innerHeight, BoundingBox bb)
	{
		BoundingBox modifiedBB = new BoundingBox(bb.min.x, bb.min.y - innerHeight * 0.5f, bb.max.x, bb.max.y + innerHeight * 0.5f);

		SimpleRaycastHit hit = SpherecastBoundingBox(ray, radius, modifiedBB);

		if (hit != null)
		{
			return new SimpleRaycastHit(new Vector2(hit.Position.x, Mathf.Clamp(hit.Position.y, bb.min.y, bb.max.y)), hit.Normal, hit.Distance);
		}
		else
		{
			return null;
		}
	}

	public static SimpleRaycastHit RaycastLineList(Ray2D ray, Vector2[] points, Vector2[] normals)
	{
		SimpleRaycastHit result = null;

		for (int i = 0; i < normals.Length; ++i)
		{
			result = SimpleRaycastHit.NearestHit(result, RaycastLineSegment(ray, points[i], points[(i + 1) % points.Length], normals[i]));
		}

		return result;
	}

	private delegate SimpleRaycastHit CastPoint(Vector2 point);
	private delegate SimpleRaycastHit CastLine(Vector2 a, Vector2 b, Vector2 normal);

	private static SimpleRaycastHit CastLineList(CastPoint castPoint, CastLine castLine, Vector2[] points, Vector2[] normals, bool noendpoints)
	{
		SimpleRaycastHit result = null;
		
		for (int i = 0; i < normals.Length; ++i)
		{
			result = SimpleRaycastHit.NearestHit(result, castLine(points[i], points[(i + 1) % points.Length], normals[i]));

			if (!noendpoints || i > 0)
			{
				SimpleRaycastHit pointHit = castPoint(points[i]);
				SimpleRaycastHit pointFiltered = SimpleRaycastHit.FilterByNormal(pointHit, normals[i]);
				
				if (i > 0 || points.Length == normals.Length)
				{
					int prevIndex = (i == 0) ? normals.Length - 1 : i - 1;

					pointFiltered = SimpleRaycastHit.FilterByNormal(pointHit, normals[prevIndex]) ?? pointFiltered;

					if (pointFiltered != null && Vector2.Dot(Vector2Helper.Rotate90(normals[prevIndex]), pointFiltered.Normal) >= -Raycasting.DOT_ERROR_TOLERANCE)
					{
						pointFiltered = null;
					}
				}

				if (pointFiltered != null && Vector2.Dot(Vector2Helper.Rotate90(normals[i]), pointFiltered.Normal) <= Raycasting.DOT_ERROR_TOLERANCE)
				{
					pointFiltered = null;
				}
				
				result = SimpleRaycastHit.NearestHit(result, pointFiltered);
			}
		}

		if (!noendpoints && points.Length != normals.Length)
		{
			result = SimpleRaycastHit.NearestHit(
				result, 
				
				SimpleRaycastHit.FilterByNormal(
					SimpleRaycastHit.FilterByNormal(
						castPoint(points[normals.Length % points.Length]), 
						normals[normals.Length - 1]
					),
					Vector2Helper.Rotate90(-normals[normals.Length - 1])
				)
			);
		}

		return result;
	}

	public static SimpleRaycastHit SpherecastLineList(Ray2D ray, float radius, Vector2[] points, Vector2[] normals, bool noendpoints)
	{
		return CastLineList(
			x => SpherecastPoint(ray, radius, x),
			(a, b, normal) => SpherecastLineSegment(ray, radius, a, b, normal),
			points,
			normals,
			noendpoints
		);
	}
	
	public static SimpleRaycastHit CapsulecastLineList(Ray2D ray, float radius, float innerHeight, Vector2[] points, Vector2[] normals, bool noendpoints)
	{
		return CastLineList(
			x => CapsulecastPoint(ray, radius, innerHeight, x),
			(a, b, normal) => CapsulecastLineSegment(ray, radius, innerHeight, a, b, normal),
			points,
			normals,
			noendpoints
		);
	}
}

