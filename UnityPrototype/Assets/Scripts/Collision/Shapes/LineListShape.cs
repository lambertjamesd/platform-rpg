using UnityEngine;

public class LineListShape : ICollisionShape
{
	private BoundingBox boundingBox;
	private Vector2[] points;
	private Vector2[] normals;
	private bool noendpoint;

	public LineListShape(Vector2[] points, bool closed, bool noendpoint)
	{
		this.points = points;
		normals = new Vector2[closed ? points.Length : (points.Length - 1)];

		for (int i = 0; i < normals.Length; ++i)
		{
			Vector2 edgeDir = points[(i + 1) % points.Length] - points[i];
			normals[i] = Vector2Helper.Rotate90(edgeDir).normalized;
		}

		boundingBox = new BoundingBox(points);

		this.noendpoint = noendpoint;
		CollisionGroup = -1;
		CollisionLayers = ~0;
	}

	public BoundingBox GetBoundingBox()
	{
		return boundingBox;
	}
	
	public SimpleRaycastHit Raycast(Ray2D ray)
	{
		return Raycasting.RaycastLineList(ray, points, normals);
	}

	public SimpleRaycastHit Spherecast(Ray2D ray, float radius)
	{
		return Raycasting.SpherecastLineList(ray, radius, points, normals, noendpoint);
	}

	public SimpleRaycastHit CapsuleCast(Ray2D ray, float radius, float innerHeight)
	{
		return Raycasting.CapsulecastLineList(ray, radius, innerHeight, points, normals, noendpoint);
	}
	
	public SimpleOverlap Overlap(ICollisionShape other)
	{
		return null;
	}
	
	public SimpleOverlap OverlapCircle(CircleShape circle)
	{
		return null;
	}
	
	public SimpleOverlap OverlapCapsule(CapsuleShape capsule)
	{
		return null;
	}

	public SimpleOverlap OverlapBoundingBox(BoundingBox bb)
	{
		return null;
	}
	
	public int CollisionGroup { get; set; }
	public int CollisionLayers { get; set; }
	public GameObject ConnectedTo { get; set; }
}
