using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ConvexSection {
	[SerializeField]
	private Vector2[] points;
	private Vector2[] normals;
	[SerializeField]
	private Connection[] connections;
	[SerializeField]
	private Vector2 min;
	[SerializeField]
	private Vector2 max;

	private enum JointType
	{
		Convex,
		Straight,
		Concave
	}

	private static readonly float STRAIGHT_TOLERANCE = 0.001f;

	private static JointType GetJointType(Vector2 firstNormal, Vector2 secondNormal)
	{
		float crossResult = Cross2D(firstNormal, secondNormal);

		if (crossResult < -STRAIGHT_TOLERANCE)
		{
			return JointType.Convex;
		}
		else if (crossResult > STRAIGHT_TOLERANCE)
		{
			return JointType.Concave;
		}
		else
		{
			return JointType.Straight;
		}
	}

	[System.Serializable]
	private class Connection
	{
		public Connection()
		{
			adjacent = null;
			adjacentIndex = -1;
		}

		public Connection(ConvexSection adjacentSection)
		{
			this.adjacent = adjacentSection;
			adjacentIndex = -1;
		}

		public ConvexSection Adjacent
		{
			get
			{
				return adjacent;
			}

			set
			{
				adjacent = value;
			}
		}

		private ConvexSection adjacent;
		public int adjacentSectionIndex = -1;
		public int adjacentIndex;
	}

	private bool HasAdjacentSection(int index)
	{
		return connections[index] != null &&
			connections[index].Adjacent != null &&
			connections[index].adjacentIndex != -1;
	}

	private void RecalcNormals()
	{
		normals = new Vector2[points.Length];
		
		for (int i = 0; i < points.Length; ++i)
		{
			Vector2 edge = points[(i + 1) % points.Length] - points[i];
			normals[i] = new Vector2(-edge.y, edge.x).normalized;
		}
	}

	public void Initialize(ConcaveCollider collider)
	{
		RecalcNormals();

		for (int i = 0; i < connections.Length; ++i)
		{
			if (connections[i] != null && connections[i].adjacentSectionIndex != -1)
			{
				connections[i].Adjacent = collider.GetSection(connections[i].adjacentSectionIndex);
			}
		}
	}

	public void SaveAdjacentIndices(ConcaveCollider collider)
	{
		for (int i = 0; i < connections.Length; ++i)
		{
			if (connections[i] != null && connections[i].Adjacent != null)
			{
				connections[i].adjacentSectionIndex = collider.GetIndex(connections[i].Adjacent);
			}
		}
	}

	public ConvexSection(Vector2[] points, ConvexSection[] adjacentSections)
	{
		if (points != null)
		{
			this.points = points;
			this.connections = new Connection[adjacentSections.Length];

			min = points[0];
			max = points[0];

			for (int i = 0; i < points.Length; ++i)
			{
				if (adjacentSections[i] != null)
				{
					ConnectTo(i, adjacentSections[i]);
				}

				min = Vector2.Min(min, points[i]);
				max = Vector2.Max(max, points[i]);

			}
			
			RecalcNormals();
		}
	}

	private Vector2 GetPoint(int index)
	{
		return points[index % points.Length];
	}

	private int GetIndex(Vector2 point)
	{
		for (int i = 0; i < points.Length; ++i)
		{
			if (point == points[i])
			{
				return i;
			}
		}

		return -1;
	}

	private void ConnectTo(int index, ConvexSection otherSection)
	{
		int otherIndex = otherSection.GetIndex(GetPoint(index + 1));
		
		if (otherIndex != -1)
		{
			connections[index] = new Connection(otherSection);
			connections[index].adjacentIndex = otherIndex;
			
			otherSection.connections[otherIndex] = new Connection(this);
			otherSection.connections[otherIndex].adjacentIndex = index;
		}
	}

	private static float Cross2D(Vector2 a, Vector2 b)
	{
		return a.x * b.y - a.y * b.x;
	}

	private Vector2 GetNextNormal(int index)
	{
		int nextIndex = (index + 1) % points.Length;

		if (HasAdjacentSection(nextIndex))
		{
			return connections[nextIndex].Adjacent.GetNextNormal(connections[nextIndex].adjacentIndex);
		}
		else
		{
			return normals[nextIndex];
		}
	}

	private Vector2 GetPrevNormal(int index)
	{
		int prevIndex = (index + points.Length - 1) % points.Length;

		if (HasAdjacentSection(prevIndex))
		{
			return connections[prevIndex].Adjacent.GetPrevNormal(connections[prevIndex].adjacentIndex);
		}
		else
		{
			return normals[prevIndex];
		}
	}

	public bool OverlapsConvex(OverlapShape shape)
	{
		if (!shape.BoundingBoxOverlap(min, max))
		{
			return false;
		}
		
		for (int i = 0; i < points.Length; ++i)
		{
			OverlapShape.Overlap lineOverlap = shape.LineOverlap(points[i], GetPoint(i + 1), normals[i]);

			if (lineOverlap.distance <= 0.0f)
			{
				return false;
			}
			
			Vector2 pointToCheck = GetPoint(i + 1);
			
			OverlapShape.Overlap pointOverlap = shape.PointOverlap(pointToCheck);

			Vector2 nextNormal = normals[(i + 1) % points.Length];
			
			if (Vector2.Dot(normals[i], nextNormal) < 0.99f &&
				Vector2.Dot(normals[i], pointOverlap.normal) > 0.0f && 
			    Vector2.Dot(pointOverlap.normal, nextNormal) > 0.0f &&
			    pointOverlap.distance <= 0.0f)
			{
				return false;
			}
		}

		return true;
	}

	private static Vector2 RayIntersection(Vector2 posA, Vector2 dirA, Vector2 posB, Vector2 dirB)
	{
		if (Mathf.Abs(dirB.x) < Mathf.Abs(dirB.y))
		{
			Vector2 swappedIntersect = RayIntersection(
				new Vector2(posA.y, posA.x),
				new Vector2(dirA.y, dirA.x),
				new Vector2(posB.y, posB.x),
				new Vector2(dirB.y, dirB.x));

			return new Vector2(swappedIntersect.y, swappedIntersect.x);
		}
		else
		{
			float slope = dirB.y / dirB.x;
			float time = ((posA.y - posB.y) - slope * (posA.x - posB.x)) / (slope * dirA.x - dirA.y);
			return posA + dirA * time;
		}
	}

	public bool ConcaveCornerOverlap(OverlapShape shape, Vector2 cornerPoint, Vector2 normalA, Vector2 normalB, out OverlapShape.Overlap result)
	{
		Vector2 edgeDirA = new Vector2(normalA.y, -normalA.x);
		Vector2 edgeDirB = new Vector2(normalB.y, -normalB.x);

		OverlapShape.Overlap edgeAContact = shape.LineOverlap(cornerPoint - edgeDirA, cornerPoint, normalA);
		OverlapShape.Overlap edgeBContact = shape.LineOverlap(cornerPoint, cornerPoint + edgeDirB, normalB);

		Vector2 correctPosA = shape.Position + edgeAContact.normal * edgeAContact.distance;
		Vector2 correctPosB = shape.Position + edgeBContact.normal * edgeBContact.distance;

		Vector2 shapePosition = RayIntersection(correctPosA, edgeDirA, correctPosB, edgeDirB);
		bool boolResult = Vector2.Dot(shapePosition - correctPosA, edgeDirA) < 0 && 
			Vector2.Dot(shapePosition - correctPosB, edgeDirB) > 0;

		if (boolResult)
		{
			Vector2 offset = shapePosition - shape.Position;
			float offsetLength = offset.magnitude;

			if (offsetLength == 0.0f)
			{
				offsetLength = 1.0f;
			}

			result = new OverlapShape.Overlap(edgeAContact.contactPoint - edgeAContact.normal * edgeAContact.distance + offset, 
			                                  offset * (1.0f / offsetLength),
			                                  offsetLength);
		}
		else
		{
			result = new OverlapShape.Overlap();
		}

		return boolResult;
	}

	private static readonly float TINY_DISTANCE = 0.0001f;

	public OverlapShape.Overlap Collide(OverlapShape shape, SortedList<float, ConvexSection> internalOverlaps)
	{
		OverlapShape.Overlap result = new OverlapShape.Overlap();
		result.distance = float.MaxValue;

		for (int i = 0; i < points.Length; ++i)
		{
			Vector2 a = points[i];
			Vector2 b = GetPoint(i + 1);
			Vector2 edge = b - a;
			OverlapShape.Overlap lineOverlap = shape.LineOverlap(a, b, normals[i]);

			if (lineOverlap.distance > 0.0f)
			{
				if (HasAdjacentSection(i))
				{
					float key = lineOverlap.distance;

					while (internalOverlaps.ContainsKey(key))
					{
						key += TINY_DISTANCE;
					}

					internalOverlaps.Add(key, connections[i].Adjacent);
					continue;
				}

				Vector2 prevNormal = GetPrevNormal(i);
				Vector2 nextNormal = GetNextNormal(i);

				JointType prevJoint = GetJointType(prevNormal, normals[i]);
				JointType nextJoint = GetJointType(normals[i], nextNormal);

				OverlapShape.Overlap cornerOverlap = new OverlapShape.Overlap();

				if ((prevJoint == JointType.Concave && 
				    ConcaveCornerOverlap(shape, a, prevNormal, normals[i], out cornerOverlap)) ||
				    (nextJoint == JointType.Concave &&
				 	ConcaveCornerOverlap(shape, b, normals[i], nextNormal, out cornerOverlap)))
				{
					if (cornerOverlap.distance < result.distance)
					{
						result = cornerOverlap;
					}

					continue;
				}
				
				float lerpValue = Vector2.Dot(lineOverlap.contactPoint - a, edge) / edge.sqrMagnitude;

				if (lerpValue >= 0.0f && lerpValue <= 1.0f)
				{
					if (lineOverlap.distance < result.distance)
					{
						result = lineOverlap;
					}

					continue;
				}

				if (nextJoint == JointType.Convex)
				{
					OverlapShape.Overlap pointOverlap = shape.PointOverlap(b);
					
					if (Vector2.Dot(normals[i], pointOverlap.normal) > 0.0f && 
					    Vector2.Dot(pointOverlap.normal, nextNormal) > 0.0f &&
					    pointOverlap.distance <= result.distance)
					{
						result = pointOverlap;
					}
				}
			}
		}

		return result;
	}

	public Vector2 Max
	{
		get
		{
			return max;
		}
	}

	public Vector2 Min
	{
		get
		{
			return min;
		}
	}

	public void DebugDraw(Transform transform, bool showInteralEdges)
	{
		for (int i = 0; i < points.Length; ++i)
		{
			Vector2 a = points[i];
			Vector2 b = points[(i + 1) % points.Length];

			Vector3 transA = transform.TransformPoint(new Vector3(a.x, a.y));
			Vector3 transB = transform.TransformPoint(new Vector3(b.x, b.y));

			Color color = new Color(0.0f, 0.0f, 1.0f, 0.5f);

			if (HasAdjacentSection(i))
			{
				color = showInteralEdges ? new Color(1.0f, 0.0f, 0.0f, 0.5f) : Color.clear;
				
				if (showInteralEdges)
				{
					Vector2 midpoint = Vector2.Lerp(min, max, 0.5f);
					Vector2 otherMidpoint = Vector2.Lerp(connections[i].Adjacent.min, connections[i].Adjacent.max, 0.5f);
					
					Debug.DrawLine(
						transform.TransformPoint(new Vector3(midpoint.x, midpoint.y, 0.0f)), 
						transform.TransformPoint(new Vector3(otherMidpoint.x, otherMidpoint.y, 0.0f)), Color.green * 0.5f);
				}
			}

			Debug.DrawLine(transA, transB, color);
		}
	}
}

[System.Serializable]
public class ConcaveCollider {
	[SerializeField]
	private ConvexSection[] sections;
	[SerializeField]
	private Vector2 min;
	[SerializeField]
	private Vector2 max;

	public ConcaveCollider(ConvexSection[] sections)
	{
		this.sections = sections;

		if (sections.Length > 0)
		{
			min = sections[0].Min;
			max = sections[0].Max;
		}

		for (int i = 0; i < sections.Length; ++i)
		{
			min = Vector2.Min(min, sections[i].Min);
			max = Vector2.Max(max, sections[i].Max);

			sections[i].SaveAdjacentIndices(this);
		}
	}

	public ConvexSection GetSection(int index)
	{
		if (index == -1)
		{
			return null;
		}
		else
		{
			return sections[index];
		}
	}

	public int GetIndex(ConvexSection section)
	{
		for (int i = 0; i < sections.Length; ++i)
		{
			if (section == sections[i])
			{
				return i;
			}
		}

		return -1;
	}
	
	public void Initialize()
	{
		foreach (ConvexSection section in sections)
		{
			section.Initialize(this);
		}
	}
	
	public Vector2 Max
	{
		get
		{
			return max;
		}
	}
	
	public Vector2 Min
	{
		get
		{
			return min;
		}
	}

	public void DebugDraw(Transform transform, bool showInteralEdges)
	{
		foreach (ConvexSection section in sections)
		{
			section.DebugDraw(transform, showInteralEdges);
		}
	}
	
	public bool OverlapCorrectionPass(OverlapShape shape)
	{
		if (!shape.BoundingBoxOverlap (min, max))
		{
			return false;
		}

		int i = 0;

		for (i = 0; i < sections.Length; ++i)
		{
			if (sections[i].OverlapsConvex(shape))
			{
				break;
			}
		}

		if (i == sections.Length)
		{
			return false;
		}

		HashSet<ConvexSection> checkedShapes = new HashSet<ConvexSection>();
		SortedList<float, ConvexSection> sectionsToCheck = new SortedList<float, ConvexSection>();
		sectionsToCheck.Add(0, sections[i]);

		OverlapShape.Overlap currentOverlap = new OverlapShape.Overlap();
		currentOverlap.distance = float.MaxValue;

		while (sectionsToCheck.Count > 0 &&
		       sectionsToCheck.Keys[0] < currentOverlap.distance)
		{
			checkedShapes.Add(sectionsToCheck.Values[0]);

			OverlapShape.Overlap overlapCheck = sectionsToCheck.Values[0].Collide(shape, sectionsToCheck);

			if (overlapCheck.distance < currentOverlap.distance)
			{
				currentOverlap = overlapCheck;
			}

			while (sectionsToCheck.Count > 0 && checkedShapes.Contains(sectionsToCheck.Values[0]))
			{
				sectionsToCheck.RemoveAt(0);
			}
		}

		shape.MoveShape(currentOverlap.normal * (currentOverlap.distance + 0.001f));

		return true;
	}
}

[System.Serializable]
public class ConcaveColliderGroup {
	[SerializeField]
	private ConcaveCollider[] colliders;

	private static readonly int maxIterations = 10;

	public ConcaveColliderGroup(ConcaveCollider[] colliders)
	{
		this.colliders = colliders;
	}

	public void Initialize()
	{
		foreach (ConcaveCollider collider in colliders)
		{
			collider.Initialize();
		}
	}

	public void DebugDraw(Transform transform, bool showInteralEdges)
	{
		foreach (ConcaveCollider collider in colliders)
		{
			collider.DebugDraw(transform, showInteralEdges);
		}
	}

	private bool OverlapCorrectionPass(OverlapShape shape)
	{
		bool result = false;

		foreach (ConcaveCollider collider in colliders)
		{
			result = collider.OverlapCorrectionPass(shape) || result;
		}

		return result;
	}

	public void OverlapCorrection(OverlapShape shape)
	{
		int i = 0;

		while (OverlapCorrectionPass(shape) && i < maxIterations)
			++i;

		if (i == maxIterations)
		{
			Debug.LogWarning("Could not correct overlap");
		}
	}
}