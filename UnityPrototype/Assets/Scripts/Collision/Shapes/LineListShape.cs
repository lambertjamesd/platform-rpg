using UnityEngine;

class LineListShape : ICollisionShape
{
	private BoundingBox boundingBox;
	private Vector2[] points;
	private Vector2[] normals;

	public LineListShape(Vector2[] points, bool closed)
	{
		this.points = points;
		normals = new Vector2[closed ? points.Length : (points.Length - 1)];

		for (int i = 0; i < normals.Length; ++i)
		{
			Vector2 edgeDir = points[(i + 1) % points.Length] - points[i];
			normals[i] = Vector2Helper.Rotate90(edgeDir).normalized;
		}

		boundingBox = new BoundingBox(points);
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
		return Raycasting.SpherecastLineList(ray, radius, points, normals);
	}

	public SimpleRaycastHit CapsuleCast(Ray2D ray, float radius, float innerHeight)
	{
		return Raycasting.CapsulecastLineList(ray, radius, innerHeight, points, normals);
	}
}
