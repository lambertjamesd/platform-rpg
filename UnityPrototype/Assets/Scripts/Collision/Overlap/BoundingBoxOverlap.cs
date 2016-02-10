using UnityEngine;

public static class BoundingBoxOverlap
{
	public static SimpleOverlap BoundingBoxBoundingBoxOverlap(BoundingBox a, BoundingBox b)
	{
		if (a.Overlaps(b))
		{
			BoundingBox overlap = a.Intersection(b);
			Vector2 size = overlap.Size;

			if (size.x <= size.y)
			{
				float y = overlap.min.y + size.y * 0.5f;

				if (a.max.x - b.min.x <= b.max.x - a.min.x)
				{
					return new SimpleOverlap(
						new Vector2(a.max.x, y),
						new Vector2(b.min.x, y)
					);
				}
				else
				{
					return new SimpleOverlap(
						new Vector2(a.min.x, y),
						new Vector2(b.max.x, y)
					);
				}
			}
			else
			{
				float x = overlap.min.x + size.x * 0.5f;

				if (a.max.y - b.min.y <= b.max.y - a.min.y)
				{
					return new SimpleOverlap(
						new Vector2(x, a.max.y),
						new Vector2(x, b.min.y)
					);
				}
				else
				{
					return new SimpleOverlap(
						new Vector2(x, a.min.y),
						new Vector2(x, b.max.y)
					);
				}
			}
		}
		else
		{
			return null;
		}
	}

	public static SimpleOverlap BoundingBoxCapsuleOverlap(BoundingBox box, Vector2 capsuleCenter, float radius, float innerHeight)
	{
		BoundingBox modified = box;
		modified.min.y -= innerHeight * 0.5f;
		modified.max.y += innerHeight * 0.5f;

		SimpleOverlap hit = SphereOverlap.SphereBBOverlap(capsuleCenter, radius, modified);

		if (hit == null)
		{
			return null;
		}
		else
		{
			Vector2 offset = box.NearestPoint(hit.To) - hit.To;
			return new SimpleOverlap(hit.To + offset, hit.From + offset);
		}
	}
}
