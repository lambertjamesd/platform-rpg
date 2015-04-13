using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ShapeOutline {
	private Vector2[] points;
	private Vector2[] normals;
	private BoundingBox boundingBox;

	public ShapeOutline(IEnumerable<Vector2> input)
	{
		points = input.ToArray();
		normals = new Vector2[points.Length];

		boundingBox.min = points[0];
		boundingBox.max = points[0];

		for (int i = 0; i < points.Length; ++i)
		{
			Vector2 nextPoint = (i + 1 == points.Length) ? points[0] : points[i + 1];
			normals[i] = ColliderMath.RotateCCW(nextPoint - points[i]).normalized;
			boundingBox.Extend(points[i]);
		}
	}

	public int PointCount
	{
		get
		{
			return points.Length;
		}
	}

	public Vector2 GetPoint(int index)
	{
		return points[index % points.Length];
	}

	public Vector2 GetNormal(int index)
	{
		return normals[index % normals.Length];
	}

	public BoundingBox BB
	{
		get
		{
			return boundingBox;
		}
	}

	public ShapeOutline Extrude(float amount)
	{
		List<Vector2> result = new List<Vector2>();

		for (int i = 0; i < points.Length; ++i)
		{
			Vector2 prevNormal = (i == 0) ? normals[points.Length - 1] : normals[i - 1];
			Vector2 normal = normals[i];
			Vector2 point = points[i];

			float normalCross = ColliderMath.Cross2D(prevNormal, normal);

			if (normalCross <= 0.0f)
			{
				// acute or flat inner angle
				Vector2 halfNormal;

				if (Mathf.Abs(normalCross) <= ColliderMath.ZERO_TOLERANCE && 
				    Vector2.Dot(normal, prevNormal) < 0.0f)
				{
					// very very sharp angle acute inner angle
					halfNormal = ((point - GetPoint(i + points.Length - 1)).normalized + (point - GetPoint(i + 1)).normalized).normalized;
				}
				else
				{
					halfNormal = (prevNormal + normal).normalized;
				}

				result.Add(ColliderMath.Intersection(amount, prevNormal, halfNormal) + point);
				result.Add(ColliderMath.Intersection(amount, halfNormal, normal) + point);
			}
			else
			{
				// obtuse inner angle
				if (Mathf.Abs(normalCross) <= ColliderMath.ZERO_TOLERANCE)
				{
					if (Vector2.Dot(normal, prevNormal) < 0.0f)
					{
						// very very sharp obtuse inner angle
						result.Add(point + (GetPoint(i + 1) - point).normalized * amount);
					}
					else
					{
						result.Add(point + normal * amount);
					}
				}
				else
				{
					result.Add(ColliderMath.Intersection(amount, prevNormal, normal) + point);
				}
			}
		}

		return new ShapeOutline(result);
	}

	public IEnumerable<Vector2> Points
	{
		get
		{
			return points;
		}
	}
	
	public void DebugDraw(Transform parentTransform, Color debugColor)
	{
		Gizmos.color = debugColor;

		Vector3 lastPoint = parentTransform.TransformPoint(points[points.Length - 1].x, points[points.Length - 1].y, 0.0f);
		for (int i = 0; i < points.Length; ++i)
		{
			Vector3 point = parentTransform.TransformPoint(points[i].x, points[i].y, 0.0f);
			Gizmos.DrawLine(lastPoint, point);
			lastPoint = point;
		}
	}
}
