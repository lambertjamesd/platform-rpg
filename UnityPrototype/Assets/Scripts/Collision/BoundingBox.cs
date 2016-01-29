using UnityEngine;
using System.Collections;

[System.Serializable]
public struct BoundingBox {
	public Vector2 min;
	public Vector2 max;

	public BoundingBox(Vector2[] points)
	{
		min = points[0];
		max = points[0];

		for (int i = 0; i < points.Length; ++i)
		{
			Extend(points[i]);
		}
	}

	public BoundingBox(float minX, float minY, float maxX, float maxY) : 
		this(new Vector2(minX, minY), new Vector2(maxX, maxY))
	{

	}

	public BoundingBox(Vector2 pointa, Vector2 pointb)
	{
		min = pointa;
		max = pointa;
		Extend(pointb);
	}

	public void Extend(Vector2 point)
	{
		min = Vector2.Min(min, point);
		max = Vector2.Max(max, point);
	}

	public void Extend(float amount)
	{
		min.x -= amount;
		min.y -= amount;
		max.x += amount;
		max.y += amount;
	}

	public BoundingBox Union(BoundingBox other)
	{
		BoundingBox result = this;
		result.Extend(other.min);
		result.Extend(other.max);
		return result;
	}

	public BoundingBox Intersection(BoundingBox other)
	{
		Vector2 resultMin = Vector2.Max(min, other.min);
		Vector2 resultMax = Vector2.Min(max, other.max);
		resultMax = Vector2.Max(resultMax, resultMin);
		return new BoundingBox(resultMin, resultMax);
	}

	public bool Overlaps(BoundingBox other)
	{
		return min.x <= other.max.x && min.y <= other.max.y &&
			max.x >= other.min.x && max.y >= other.min.y;
	}

	public Vector2 Lerp(Vector2 input)
	{
		return new Vector3(
			Mathf.Lerp(min.x, max.y, input.x),
			Mathf.Lerp(min.y, max.y, input.y)
		);
	}

	public BoundingBox[] Subdivide(Vector2 cutPoint)
	{
		Vector2 lerpedPoint = Lerp (cutPoint);

		return new BoundingBox[]{
			new BoundingBox(min, lerpedPoint),
			new BoundingBox(new Vector2(lerpedPoint.x, min.y), new Vector2(max.x, lerpedPoint.y)),
			new BoundingBox(new Vector2(min.x, lerpedPoint.y), new Vector2(lerpedPoint.x, max.y)),
			new BoundingBox(lerpedPoint, max)
		};
	}

	public Vector2 CornerInDirection(Vector2 direction)
	{
		return new Vector2(
			(direction.x == 0.0f) ? (min.x + max.x) * 0.5f : (direction.x < 0.0f) ? min.x : max.x,
			(direction.y == 0.0f) ? (min.y + max.y) * 0.5f : (direction.y < 0.0f) ? min.y : max.y
		);
	}
}
