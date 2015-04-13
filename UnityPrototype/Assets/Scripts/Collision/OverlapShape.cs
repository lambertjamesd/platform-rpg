using UnityEngine;
using System.Collections;

public abstract class OverlapShape {

	protected OverlapShape(Vector2 position)
	{
		this.position = position;
	}
	
	protected Vector2 position;
	protected BoundingBox boundingBox;
	private bool boundingBoxDirty = true;

	public Vector2 Position
	{
		get
		{
			return position;
		}
	}

	public BoundingBox BB
	{
		get
		{
			if (boundingBoxDirty)
			{
				boundingBox = CalcBoundingBox();
				boundingBoxDirty = false;
			}

			return boundingBox;
		}
	}

	public struct Overlap
	{
		public Overlap(Vector2 contactPoint, Vector2 normal, float distance)
		{
			this.contactPoint = contactPoint;
			this.normal = normal;
			this.distance = distance;
		}

		public Vector2 contactPoint;
		public Vector2 normal;
		public float distance;
	}
	
	protected abstract BoundingBox CalcBoundingBox();
	public abstract Overlap LineOverlap(Vector2 lineA, Vector2 lineB, Vector2 lineNormal);
	public abstract Overlap PointOverlap(Vector2 point);
	public void MoveShape(Vector2 amount)
	{
		position += amount;
	}
}


public class PointOverlapShape : OverlapShape {

	public PointOverlapShape(Vector2 position) : base(position)
	{

	}

	protected override BoundingBox CalcBoundingBox()
	{
		return new BoundingBox(position, position);
	}
	
	public override OverlapShape.Overlap LineOverlap(Vector2 lineA, Vector2 lineB, Vector2 lineNormal)
	{
		Vector2 edge = lineB - lineA;
		Vector2 contactPoint = edge * Vector2.Dot(position - lineA, edge) / Vector2.Dot(edge, edge) + lineA;
		return new OverlapShape.Overlap(contactPoint, lineNormal, Vector2.Dot(lineA - position, lineNormal));
	}

	public override OverlapShape.Overlap PointOverlap(Vector2 point)
	{
		Vector2 offset = position - point;
		float distance = offset.magnitude;

		if (distance == 0.0f)
		{
			return new OverlapShape.Overlap(point, Vector2.zero, 0.0f);
		}
		else
		{
			return new OverlapShape.Overlap(point, offset * (1.0f / distance), -distance);
		}
	}
}


public class SphereOverlapShape : OverlapShape {
	
	public SphereOverlapShape(Vector2 position, float radius) : base(position)
	{
		this.radius = radius;
	}

	private float radius;

	public float Radius
	{
		get
		{
			return radius;
		}
	}
	
	protected override BoundingBox CalcBoundingBox()
	{
		return new BoundingBox(position - Vector2.one * radius, position + Vector2.one * radius);
	}

	public static OverlapShape.Overlap SphereLineOverlap(Vector2 spherePos, float sphereRadius, Vector2 lineA, Vector2 lineB, Vector2 lineNormal)
	{
		Vector2 edge = lineB - lineA;
		Vector2 contactPoint = edge * Vector2.Dot(spherePos - lineA, edge) / Vector2.Dot(edge, edge) + lineA;
		return new OverlapShape.Overlap(contactPoint, lineNormal, Vector2.Dot(lineA - spherePos, lineNormal) + sphereRadius);
	}

	public override OverlapShape.Overlap LineOverlap(Vector2 lineA, Vector2 lineB, Vector2 lineNormal)
	{
		return SphereLineOverlap(position, radius, lineA, lineB, lineNormal);
	}
	
	public override OverlapShape.Overlap PointOverlap(Vector2 point)
	{
		Vector2 offset = position - point;
		float distance = offset.magnitude;
		
		if (distance == 0.0f)
		{
			return new OverlapShape.Overlap(point, Vector2.zero, 0.0f);
		}
		else
		{
			return new OverlapShape.Overlap(point, offset * (1.0f / distance), radius - distance);
		}
 	}
}

public class CapsuleOverlapShape : OverlapShape {
	public CapsuleOverlapShape(Vector2 position, Vector2 up, float height, float radius) : base(position)
	{
		this.up = up;
		this.centerOffset = Mathf.Max(0.0f, height * 0.5f - radius);
		this.radius = radius;
		this.maxExtent = radius + centerOffset;
	}

	private Vector2 up;
	private float centerOffset;
	private float radius;
	private float maxExtent;
	
	
	protected override BoundingBox CalcBoundingBox()
	{
		return new BoundingBox(position - Vector2.one * maxExtent, position + Vector2.one * maxExtent);
	}
	
	public override OverlapShape.Overlap LineOverlap(Vector2 lineA, Vector2 lineB, Vector2 lineNormal)
	{
		OverlapShape.Overlap overlapA = SphereOverlapShape.SphereLineOverlap(position + up * centerOffset, radius, lineA, lineB, lineNormal);
		OverlapShape.Overlap overlapB = SphereOverlapShape.SphereLineOverlap(position - up * centerOffset, radius, lineA, lineB, lineNormal);

		if (overlapA.distance > overlapB.distance)
		{
			return overlapA;
		}
		else
		{
			return overlapB;
		}
	}
	
	public override OverlapShape.Overlap PointOverlap(Vector2 point)
	{
		float projectionDistance = Mathf.Clamp(Vector2.Dot(point - position, up), -centerOffset, centerOffset);
		Vector2 closestPoint = position + up * projectionDistance;

		Vector2 offset = closestPoint - point;
		float distance = offset.magnitude;
		
		if (distance == 0.0f)
		{
			return new OverlapShape.Overlap(point, Vector2.zero, 0.0f);
		}
		else
		{
			return new OverlapShape.Overlap(point, offset * (1.0f / distance), radius - distance);
		}
	}
}