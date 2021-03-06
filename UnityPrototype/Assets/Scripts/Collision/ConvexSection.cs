﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ConvexSection {
	private bool[] isBorder;
	[SerializeField]
	private Vector2[] points;
	private Vector2[] normals;
	private Connection[] connections;
	[SerializeField]
	private ConnectionSerialData[] connectionSerialData;
	
	private BoundingBox boundingBox;
	
	private enum JointType
	{
		Convex,
		Straight,
		Concave
	}
	
	private static readonly float STRAIGHT_TOLERANCE = 0.001f;
	
	private static JointType GetJointType(Vector2 firstNormal, Vector2 secondNormal)
	{
		float crossResult = ColliderMath.Cross2D(firstNormal, secondNormal);
		
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
	private class ConnectionSerialData
	{
		public int adjacentSectionIndex = -1;
		public int adjacentIndex;
	}

	private class Connection
	{
		public Connection()
		{
			adjacent = null;
		}
		
		public Connection(ConvexSection adjacentSection)
		{
			this.adjacent = adjacentSection;
		}
		
		public ConvexSection Adjacent
		{
			get
			{
				return adjacent;
			}
		}
		
		private ConvexSection adjacent;
	}

	public void MarkBorder(BoundingBox levelBB)
	{
		for (int i = 0; i < points.Length; ++i)
		{
			if (Mathf.Abs(points[i].x - levelBB.min.x) < 0.1f)
			{
				isBorder[i] = true;
				boundingBox.min.x = float.NegativeInfinity;
			}
			if (Mathf.Abs(points[i].x - levelBB.max.x) < 0.1f)
			{
				isBorder[i] = true;
				boundingBox.max.x = float.PositiveInfinity;
			}
			if (Mathf.Abs(points[i].y - levelBB.min.y) < 0.1f)
			{
				isBorder[i] = true;
				boundingBox.min.y = float.NegativeInfinity;
			}
		}
	}

	
	public void ReconnectSections(ConcaveCollider collider)
	{
		RecalcNormals();
		isBorder = new bool[points.Length];

		if (connectionSerialData != null)
		{
			connections = new Connection[connectionSerialData.Length];
			
			for (int i = 0; i < connectionSerialData.Length; ++i)
			{
				if (connectionSerialData[i].adjacentSectionIndex != -1)
				{
					connections[i] = new Connection(collider.GetSection(connectionSerialData[i].adjacentSectionIndex));
				}
				else
				{
					connections[i] = new Connection();
				}
			}
		}
	}
	
	public bool HasAdjacentSection(int index)
	{
		return connections != null &&
			connections[index] != null &&
			connections[index].Adjacent != null &&
				connectionSerialData[index].adjacentIndex != -1;
	}

	public ConvexSection GetAdjacentSection(int index)
	{
		return connections[index] == null ? null : connections[index].Adjacent;
	}

	public int GetAdjacentSectionIndex(int index)
	{
		return connections[index] == null ? -1 : connectionSerialData[index].adjacentIndex;
	}
	
	public void RecalcBoundingBox()
	{
		boundingBox.min = points[0];
		boundingBox.max = points[0];
		
		for (int i = 0; i < points.Length; ++i)
		{
			boundingBox.Extend(points[i]);
		}
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
	
	public void SaveAdjacentIndices(ConcaveCollider collider)
	{
		for (int i = 0; i < connections.Length; ++i)
		{
			if (connections[i] != null && connections[i].Adjacent != null)
			{
				connectionSerialData[i].adjacentSectionIndex = collider.GetIndex(connections[i].Adjacent);
			}
		}
	}
	
	public ConvexSection(Vector2[] points, ConvexSection[] adjacentSections)
	{
		if (points != null)
		{
			this.points = points;
			this.connections = new Connection[adjacentSections.Length];
			this.connectionSerialData = new ConnectionSerialData[adjacentSections.Length];

			for (int i = 0; i < adjacentSections.Length; ++i)
			{
				this.connectionSerialData[i] = new ConnectionSerialData();
			}
			
			boundingBox.min = points[0];
			boundingBox.max = points[0];
			
			for (int i = 0; i < points.Length; ++i)
			{
				if (adjacentSections[i] != null)
				{
					ConnectTo(i, adjacentSections[i]);
				}
				
				boundingBox.Extend(points[i]);
				
			}
			
			RecalcNormals();

			isBorder = new bool[points.Length];
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
		return normals[index % points.Length];
	}

	public bool IsBorder(int index)
	{
		return isBorder[index % points.Length];
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
			connectionSerialData[index].adjacentIndex = otherIndex;
			
			otherSection.connections[otherIndex] = new Connection(this);
			otherSection.connectionSerialData[otherIndex].adjacentIndex = index;
		}
	}

	private Vector2 GetNextPoint(int index)
	{
		index = index % points.Length;
		int nextIndex = (index + 1) % points.Length;

		if (HasAdjacentSection(index))
		{
			return connections[index].Adjacent.GetNextPoint(connectionSerialData[index].adjacentIndex + 1);
		}
		else
		{
			return points[nextIndex];
		}
	}
	
	private Vector2 GetPrevPoint(int index)
	{
		int prevIndex = (index + points.Length - 1) % points.Length;
		
		if (HasAdjacentSection(prevIndex))
		{
			return connections[prevIndex].Adjacent.GetPrevPoint(connectionSerialData[prevIndex].adjacentIndex);
		}
		else
		{
			return points[prevIndex];
		}
	}
	
	private Vector2 GetNextNormal(int index)
	{
		int nextIndex = (index + 1) % points.Length;
		
		if (HasAdjacentSection(nextIndex))
		{
			return connections[nextIndex].Adjacent.GetNextNormal(connectionSerialData[nextIndex].adjacentIndex);
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
			return connections[prevIndex].Adjacent.GetPrevNormal(connectionSerialData[prevIndex].adjacentIndex);
		}
		else
		{
			return normals[prevIndex];
		}
	}
	
	public bool OverlapsConvex(OverlapShape shape)
	{
		if (!shape.BB.Overlaps(boundingBox))
		{
			return false;
		}
		
		for (int i = 0; i < points.Length; ++i)
		{
			if (!IsBorder(i + 1))
			{
				if (!IsBorder(i))
				{
					OverlapShape.Overlap lineOverlap = shape.LineOverlap(points[i], GetPoint(i + 1), normals[i]);
					
					if (lineOverlap.distance <= 0.0f)
					{
						return false;
					}
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
			bool aIsBorder = isBorder[i];
			bool bIsBorder = IsBorder(i + 1);
			Vector2 a = points[i];
			Vector2 b = GetPoint(i + 1);
			Vector2 edge = b - a;
			OverlapShape.Overlap lineOverlap = shape.LineOverlap(a, b, normals[i]);
			
			if (lineOverlap.distance > 0.0f && (!aIsBorder || !bIsBorder))
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
				
				if ((lerpValue >= 0.0f || aIsBorder) && (lerpValue <= 1.0f || bIsBorder))
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
	
	public BoundingBox BB
	{
		get
		{
			return boundingBox;
		}
	}

	public void BuildShapes(List<LineListShape> output)
	{
		List<Vector2> firstPointList = new List<Vector2>();
		List<Vector2> currentPointList = firstPointList;

		for (int i = 0; i < points.Length; ++i)
		{
			Vector2 prevPoint = GetPrevPoint(i);
			Vector2 nextPoint = GetNextPoint(i);

			bool prevMatches = GetPoint(i + points.Length - 1) == prevPoint;
			bool nextMatches = GetPoint(i + 1) == nextPoint;

			if (!prevMatches)
			{
				currentPointList.Add(prevPoint);
			}

			if (prevMatches || nextMatches)
			{
				currentPointList.Add(points[i]);
			}

			if (!nextMatches)
			{
				currentPointList.Add(nextPoint);

				if (currentPointList != firstPointList && currentPointList.Count > 2)
				{
					output.Add(new LineListShape(currentPointList.ToArray(), false, true));
				}

				currentPointList = new List<Vector2>();
			}
		}

		if (firstPointList == currentPointList)
		{
			output.Add(new LineListShape(firstPointList.ToArray(), true, false));
		}
		else
		{
			currentPointList.AddRange(firstPointList);

			if (currentPointList.Count > 2)
			{
				output.Add(new LineListShape(currentPointList.ToArray(), false, true));
			}
		}
	}
	
	public void DebugDraw(Transform transform, bool showInteralEdges, Color edgeColor)
	{
		for (int i = 0; i < points.Length; ++i)
		{
			Vector2 a = points[i];
			Vector2 b = points[(i + 1) % points.Length];
			
			Vector3 transA = transform.TransformPoint(new Vector3(a.x, a.y));
			Vector3 transB = transform.TransformPoint(new Vector3(b.x, b.y));
			
			Color color = edgeColor;
			
			if (HasAdjacentSection(i))
			{
				color = showInteralEdges ? new Color(1.0f, 0.0f, 0.0f, 0.5f) : Color.clear;
				
				if (showInteralEdges)
				{
					Vector2 midpoint = Vector2.Lerp(boundingBox.min, boundingBox.max, 0.5f);
					Vector2 otherMidpoint = Vector2.Lerp(connections[i].Adjacent.boundingBox.min, connections[i].Adjacent.boundingBox.max, 0.5f);
					
					Debug.DrawLine(
						transform.TransformPoint(new Vector3(midpoint.x, midpoint.y, 0.0f)), 
						transform.TransformPoint(new Vector3(otherMidpoint.x, otherMidpoint.y, 0.0f)), Color.green * 0.5f);
				}
			}
			else if (IsBorder(i) && IsBorder(i + 1))
			{
				color = Color.green;
			}
			
			Debug.DrawLine(transA, transB, color);
		}
	}
}
