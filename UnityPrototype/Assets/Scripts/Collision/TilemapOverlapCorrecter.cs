using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MeshOutlineGenerator
{
	private static readonly float EQUALITY_TOLERANCE = 0.01f;

	private class Loop
	{
		public List<Vector2> points = new List<Vector2>();

		private static readonly float SAME_ANGLE_TOLERANCE = 0.5f;
		private static readonly float POINT_COLLIDE_TOLERANCE = 0.05f;

		private bool isClosed = false;

		public Loop()
		{

		}

		public Loop(IEnumerable<Vector2> points)
		{
			this.points = points.ToList();
			this.isClosed = true;
		}

		public void JoinSegment(Vector2 a, Vector2 b, int endpoint)
		{
			if (points.Count == 0)
			{
				points.Add(a);
				points.Add(b);
			}
			else if (endpoint == 1)
			{
				points.Add(b);
			}
			else
			{
				points.Insert(0, a);
			}
		}

		public void CombineLoop(Loop otherLoop, int endpoint)
		{
			if (endpoint == 1)
			{
				points.AddRange(otherLoop.points);
			}
			else
			{
				points.InsertRange(0, otherLoop.points);
			}
		}

		public void Close()
		{
			isClosed = true;
		}

		public int EndpointMatch(Vector2 point)
		{
			if (points.Count > 0 && !isClosed)
			{
				if ((points[0] - point).sqrMagnitude < EQUALITY_TOLERANCE * EQUALITY_TOLERANCE)
				{
					return -1;
				}
				else if ((points[points.Count - 1] - point).sqrMagnitude < EQUALITY_TOLERANCE * EQUALITY_TOLERANCE)
				{
					return 1;
				}
			}

			return 0;
		}

		public void Simplify()
		{
			int i = 0;

			float cosSameAngle = Mathf.Cos(Mathf.Deg2Rad * SAME_ANGLE_TOLERANCE);

			while (i < points.Count)
			{
				Vector2 a = points[i];
				Vector2 b = points[(i + 1) % points.Count];
				Vector2 c = points[(i + 2) % points.Count];

				if (Vector2.Dot((c - b).normalized, (b - a).normalized) > cosSameAngle)
				{
					points.RemoveAt((i + 1) % points.Count);
				}
				else
				{
					++i;
				}
			}
		}
		
		private bool IsConvexJoint(int prevIndex, int jointIndex, int nextIndex)
		{
			Vector2 lastPoint = GetPoint(prevIndex);
			Vector2 point = GetPoint(jointIndex);
			Vector2 nextPoint = GetPoint(nextIndex);

			Vector2 nextEdge = nextPoint - point;
			Vector2 previousEdge = point - lastPoint;
			
			return ColliderMath.Cross2D(previousEdge, nextEdge) < EQUALITY_TOLERANCE;
		}

		private bool IsConvexJoint(int jointIndex)
		{
			return IsConvexJoint(jointIndex - 1, jointIndex, jointIndex + 1);
		}

		private int GetPointIndex(int loopIndex)
		{
			while (loopIndex < 0)
			{
				loopIndex += points.Count;
			}

			return loopIndex % points.Count;
		}

		private Vector2 GetPoint(int loopIndex)
		{
			return points[GetPointIndex(loopIndex)];
		}

		private bool IsLinear(int prevIndex, int jointIndex, int nextIndex)
		{
			Vector2 lastPoint = GetPoint(prevIndex);
			Vector2 point = GetPoint(jointIndex);
			Vector2 nextPoint = GetPoint(nextIndex);
			
			Vector2 nextEdge = nextPoint - point;
			Vector2 previousEdge = point - lastPoint;

			return Vector3.Dot(nextEdge.normalized, previousEdge.normalized) > Mathf.Cos(SAME_ANGLE_TOLERANCE * Mathf.Deg2Rad);
		}

		private bool IsSectionEmtpy(int start, int end)
		{
			int pointCheckStart = end + 1;
			int nextCycleStart = start + points.Count - 1;

			while (IsLinear(pointCheckStart - 1, pointCheckStart, pointCheckStart + 1))
			{
				++pointCheckStart;
			}
			
			while (IsLinear(nextCycleStart - 1, nextCycleStart, nextCycleStart + 1))
			{
				--nextCycleStart;
			}

			for (int externalIndex = pointCheckStart; externalIndex <= nextCycleStart; ++externalIndex)
			{
				Vector2 externalPoint = GetPoint(externalIndex);

				bool isPointOutside = false;

				for (int segmentStart = start; segmentStart <= end; ++segmentStart)
				{
					Vector2 pointA = GetPoint(segmentStart);
					Vector2 pointB = GetPoint((segmentStart + 1 > end) ? start : (segmentStart + 1));

					Vector2 segment = pointB - pointA;
					Vector2 normal = new Vector2(-segment.y, segment.x).normalized;

					if (Vector2.Dot(normal, externalPoint - pointA) > POINT_COLLIDE_TOLERANCE)
					{
						isPointOutside = true;
						break;
					}
				}

				if (!isPointOutside)
				{
					return false;
				}
			}

			return true;
		}

		public void BoundingBox(out Vector2 min, out Vector2 max)
		{
			min = points[0];
			max = points[0];

			foreach (Vector2 point in points)
			{
				min = Vector2.Min(min, point);
				max = Vector2.Max(max, point);
			}
		}

		public ConcaveCollider BuildCollider()
		{
			List<ConvexSection> sections = new List<ConvexSection>();
			List<ConvexSection> adjacentSections = new List<ConvexSection>(points.Count);

			for (int i = 0; i < points.Count; ++i)
			{
				adjacentSections.Add(null);
			}

			int currentIndex = 0;

			int failedAttempts = 0;

			while (points.Count > 2 && failedAttempts < 100)
			{
				int startIndex = currentIndex;
				int endIndex = currentIndex + 2;
				
				++failedAttempts;
				
				while (IsConvexJoint(endIndex - 1) && 
				       IsConvexJoint(endIndex, startIndex, startIndex + 1) &&
				       IsSectionEmtpy(startIndex, endIndex) &&
				       GetPoint(startIndex) != GetPoint(endIndex))
				{
					++endIndex;
				}

				--endIndex;

				while (IsConvexJoint(startIndex + 1) &&
				       IsConvexJoint(endIndex - 1, endIndex, startIndex) &&
				       IsSectionEmtpy(startIndex, endIndex) &&
				       GetPoint(startIndex) != GetPoint(endIndex))
				{
					--startIndex;
				}

				++startIndex;

				if (endIndex - startIndex > 2)
				{
					currentIndex = GetPointIndex(endIndex);

					int moduloStart = GetPointIndex(startIndex);
					int moduloEnd = GetPointIndex(endIndex);

					List<Vector2> convexSection = null;
					List<ConvexSection> convexConnections = null;

					if (moduloStart == moduloEnd)
					{
						convexSection = points.GetRange(0, points.Count);
						convexConnections = adjacentSections.GetRange(0, points.Count);

						points.Clear();
						adjacentSections.Clear();
					}
					else if (moduloStart < moduloEnd)
					{
						int count = moduloEnd - moduloStart;

						convexSection = points.GetRange(moduloStart, count + 1);
						convexConnections = adjacentSections.GetRange(moduloStart, count + 1);
						points.RemoveRange(moduloStart + 1, count - 1);
						adjacentSections.RemoveRange(moduloStart + 1, count - 1);
					}
					else
					{
						int endCount = points.Count - moduloStart;
						convexSection = points.GetRange(moduloStart, endCount);
						convexConnections = adjacentSections.GetRange(moduloStart, endCount);

						convexSection.AddRange(points.GetRange(0, moduloEnd + 1));
						convexConnections.AddRange(adjacentSections.GetRange(0, moduloEnd + 1));

						if (moduloStart + 1 < points.Count)
						{
							points.RemoveRange(moduloStart + 1, endCount  - 1);
							adjacentSections.RemoveRange(moduloStart + 1, endCount - 1);
						}

						points.RemoveRange(0, moduloEnd);
						adjacentSections.RemoveRange(0, moduloEnd);

						moduloStart -= moduloEnd;
					}

					if (convexConnections.Count > 0)
					{
						ConvexSection newSection = new ConvexSection(convexSection.ToArray(), convexConnections.ToArray());

						if (adjacentSections.Count > 0)
						{
							adjacentSections[moduloStart] = newSection;
						}

						sections.Add(newSection);
						failedAttempts = 0;
					}
				}
				else
				{
					++currentIndex;
				}
			}

			if (points.Count > 2)
			{
				Debug.LogError("Failed convex decompisition");
			}

			return new ConcaveCollider(sections.ToArray());
		}
	}

	private List<Loop> loops = new List<Loop>();
	private ConcaveColliderGroup result = null;

	private Loop GetLoop(Vector2 a, out int end)
	{
		foreach (Loop loop in loops)
		{
			int endpoint = loop.EndpointMatch(a);

			if (endpoint != 0)
			{
				end = endpoint;
				return loop;
			}
		}

		end = 0;
		return null;
	}

	private void AddToLoop(Vector2 a, Vector2 b)
	{
		int loopEndA = 0;
		int loopEndB = 0;
		Loop loopA = GetLoop(a, out loopEndA);
		Loop loopB = GetLoop(b, out loopEndB);

		if (loopA == null && loopB == null)
		{
			loopA = new Loop();
			loopA.JoinSegment(a, b, -1);
			loops.Add(loopA);
		}
		else if (loopA != null && loopB == null)
		{
			loopA.JoinSegment(a, b, loopEndA);
		}
		else if (loopA == null && loopB != null)
		{
			loopB.JoinSegment(a, b, loopEndB);
		}
		else
		{
			if (loopA == loopB)
			{
				loopA.Close();
			}
			else
			{
				loopA.CombineLoop(loopB, 1);

				loops.Remove(loopB);
			}
		}
	}

	private void GenerateSegments(Mesh mesh, Plane plane, Vector3 right)
	{
		Vector3 up = Vector3.Cross(plane.normal, right).normalized;

		Vector3[] positions = mesh.vertices;
		int[] indices = mesh.GetTriangles(0);

		Vector2[] crossings = new Vector2[3];

		for (int i = 0; i + 2 < indices.Length; i += 3)
		{
			int crossingIndex = 0;

			for (int j = 0; j < 3; ++j)
			{
				int currentIndex = indices[i + j];
				int nextIndex = indices[i + (j + 1) % 3];
				float distanceA = plane.GetDistanceToPoint(positions[currentIndex]);
				float distanceB = plane.GetDistanceToPoint(positions[nextIndex]);
				bool doesCross = distanceA * distanceB < 0;

				if (doesCross)
				{
					float lerp = -distanceA / (distanceB - distanceA);
					Vector3 lerped = Vector3.Lerp(positions[currentIndex], positions[nextIndex], lerp);
					crossings[crossingIndex] = new Vector2(Vector3.Dot(lerped, right), Vector3.Dot(lerped, up));
					++crossingIndex;
				}
			}

			if (crossingIndex == 2 && (crossings[0] - crossings[1]).sqrMagnitude >= EQUALITY_TOLERANCE * EQUALITY_TOLERANCE)
			{
				Vector3 faceNormal = Vector3.Cross(positions[indices[i + 2]] - positions[indices[i]], positions[indices[i + 1]] - positions[indices[i]]);
				Vector2 normal2D = new Vector2(Vector3.Dot(faceNormal, right), Vector3.Dot(faceNormal, up));

				Vector2 segmentDirection = crossings[1] - crossings[0];
				Vector2 segmentNormal = new Vector2(segmentDirection.y, -segmentDirection.x);

				if (Vector2.Dot(segmentNormal, normal2D) > 0)
				{
					AddToLoop(crossings[0], crossings[1]);
				}
				else
				{
					AddToLoop(crossings[1], crossings[0]);
				}
			}
		}
	}

	public void SimplifyLoops()
	{
		foreach (Loop loop in loops)
		{
			loop.Simplify();
		}
	}

	public void BuildCollider()
	{
		List<ConcaveCollider> colliders = new List<ConcaveCollider>();

		foreach (Loop loop in loops)
		{
			colliders.Add(loop.BuildCollider());
		}

		result = new ConcaveColliderGroup(colliders.ToArray());
	}

	public MeshOutlineGenerator(Mesh mesh, Plane plane, Vector3 right)
	{
		GenerateSegments(mesh, plane, right);
		SimplifyLoops();
		BuildCollider();
	}

	public MeshOutlineGenerator(List<ShapeOutline> outlines)
	{
		foreach (ShapeOutline shape in outlines)
		{
			loops.Add(new Loop(shape.Points));
		}

		SimplifyLoops();
		BuildCollider();
	}

	public ConcaveColliderGroup Result
	{
		get
		{
			return result;
		}
	}

	private void DrawLine(Vector2 a, Vector2 b, Color color)
	{
		Debug.DrawLine(new Vector3(a.x, a.y, 0.0f), new Vector3(b.x, b.y, 0.0f), color);
	}

	public void DebugDraw(Transform transform, bool showInternalEdges)
	{
		result.DebugDraw(transform, showInternalEdges, new Color(0.0f, 0.0f, 1.0f, 0.5f));
	}
}

public class TilemapOverlapCorrecter : MonoBehaviour {

	public Mesh meshToOutline;
	public bool debugDrawInternalEdges;
	[SerializeField]
	private ConcaveColliderGroup colliderGroup;
	private SpacialIndex spacialIndex;

	public List<CharacterSize> pathingNetworkSizes = new List<CharacterSize>();

	[SerializeField]
	private List<PathingNetwork> pathingNetworks = new List<PathingNetwork>();

	private Ray pickerRay;

	public bool debugDrawPathing;

	void Awake () {
		if (colliderGroup != null)
		{
			spacialIndex = new SpacialIndex(colliderGroup.BoundingBox);

			foreach (LineListShape shape in colliderGroup.BuildShapes())
			{
				spacialIndex.IndexShape(shape);
			}
		}
	}

	public void Rebuild()
	{
		if (meshToOutline != null)
		{
			colliderGroup = new MeshOutlineGenerator(meshToOutline, new Plane(Vector3.forward, Vector3.forward * 0.5f), Vector3.right).Result;

			pathingNetworks = new List<PathingNetwork>();
			List<ShapeOutline> outlines = colliderGroup.GetOutline();

			for (int i = 0; i < pathingNetworkSizes.Count; ++i)
			{
				pathingNetworks.Add(new PathingNetwork(outlines, pathingNetworkSizes[i]));
			}
		}
	}

	public void VoxelMapBaked()
	{
		MeshCollider[] meshColliders = transform.FindChild("Static").gameObject.GetComponentsInChildren<MeshCollider>();
		CombineInstance[] combineInstances = new CombineInstance[meshColliders.Length];

		for (int i = 0; i < meshColliders.Length; ++i)
		{
			CombineInstance instance = new CombineInstance();
			instance.mesh = meshColliders[i].sharedMesh;
			instance.transform = transform.worldToLocalMatrix * meshColliders[i].transform.localToWorldMatrix;
			instance.subMeshIndex = 0;
			combineInstances[i] = instance;
		}

		meshToOutline = new Mesh();

		meshToOutline.CombineMeshes(combineInstances);

		Rebuild();
	}
	
	public Ray PickerRay
	{
		get
		{
			return pickerRay;
		}
		
		set
		{
			pickerRay = value;
		}
	}

	void OnDrawGizmosSelected()
	{
		if (colliderGroup != null)
		{
			colliderGroup.DebugDraw(transform, debugDrawInternalEdges, new Color(0.0f, 0.0f, 1.0f, 0.5f));
		}

		if (debugDrawPathing && pathingNetworks != null)
		{
			foreach (PathingNetwork network in pathingNetworks)
			{
				network.DebugDraw(transform, network.SelectNode(pickerRay));
			}
		}
	}

	public Vector3 CorrectPointOverlap(Vector3 worldPoint)
	{
		Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

		if (colliderGroup != null)
		{
			PointOverlapShape pointShape = new PointOverlapShape(new Vector2(localPoint.x, localPoint.y));
			colliderGroup.OverlapCorrection(pointShape);

			localPoint = new Vector3(pointShape.Position.x, pointShape.Position.y, localPoint.z);
		}

		return transform.TransformPoint(localPoint);
	}
	
	public Vector3 CorrectSphereOverlap(Vector3 worldPoint, float radius)
	{
		Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
		
		if (colliderGroup != null)
		{
			SphereOverlapShape pointShape = new SphereOverlapShape(new Vector2(localPoint.x, localPoint.y), radius);
			colliderGroup.OverlapCorrection(pointShape);
			
			localPoint = new Vector3(pointShape.Position.x, pointShape.Position.y, localPoint.z);
		}
		
		return transform.TransformPoint(localPoint);
	}
	
	public Vector3 CorrectCapsuleOverlap(Vector3 worldPoint, Vector3 worldUp, float height, float radius)
	{
		Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
		Vector3 localUp = transform.InverseTransformDirection(worldUp);
		
		if (colliderGroup != null)
		{
			CapsuleOverlapShape pointShape = new CapsuleOverlapShape(new Vector2(localPoint.x, localPoint.y), new Vector2(localUp.x, localUp.y), height, radius);
			colliderGroup.OverlapCorrection(pointShape);
			localPoint = new Vector3(pointShape.Position.x, pointShape.Position.y, localPoint.z);
		}
		
		return transform.TransformPoint(localPoint);
	}

	public SpacialIndex GetSpacialIndex()
	{
		return spacialIndex;
	}

	public void OnDrawGizmos()
	{
		if (spacialIndex != null)
		{
			Gizmos.color = Color.cyan;
			spacialIndex.DrawGizmos();
		}
	}
}
