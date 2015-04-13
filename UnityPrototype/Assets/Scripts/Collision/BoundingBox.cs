using UnityEngine;
using System.Collections;

[System.Serializable]
public struct BoundingBox {
	public Vector2 min;
	public Vector2 max;

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
}
