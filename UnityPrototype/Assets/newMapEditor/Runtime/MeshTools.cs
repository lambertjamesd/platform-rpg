using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct MeshVertex
{
	public Vector3 position;
	public Vector3 normal;
	public Vector4 tangent;
	public Vector2 uv;
	public Vector2 uv2;
	
	public MeshVertex(Vector3 position, Vector3 normal, Vector4 tangent, Vector2 uv, Vector2 uv2)
	{
		this.position = position;
		this.normal = normal;
		this.tangent = tangent;
		this.uv = uv;
		this.uv2 = uv2;
	}
	
	public override bool Equals (object obj)
	{
		return obj is MeshVertex && this == (MeshVertex)obj;
	}

	public override int GetHashCode ()
	{
		return position.GetHashCode() ^ normal.GetHashCode() ^ tangent.GetHashCode() ^ uv.GetHashCode() ^ uv2.GetHashCode();
	}
	
	public static bool operator ==(MeshVertex a, MeshVertex b)
	{
		return a.position == b.position &&
			a.normal == b.normal &&
				a.tangent == b.tangent &&
				a.uv == b.uv &&
				a.uv2 == b.uv2;
	}
	
	public static bool operator != (MeshVertex a, MeshVertex b)
	{
		return !(a == b);
	}

	public static MeshVertex Lerp(MeshVertex a, MeshVertex b, float t)
	{
		Vector4 tangent = Vector4.Lerp(a.tangent, b.tangent, t);

		if (tangent.sqrMagnitude > 0.0f)
		{
			float actualW = tangent.w;
			tangent.w = 0.0f;
			tangent.Normalize();
			tangent.w = actualW;
		}

		return new MeshVertex(
			Vector3.Lerp(a.position, b.position, t),
			Vector3.Lerp(a.normal, b.normal, t).normalized,
			tangent,
			Vector2.Lerp(a.uv, b.uv, t),
			Vector2.Lerp(a.uv2, b.uv2, t));
	}
}

public class MeshBuilder
{
	private bool usePosition;
	private bool useNormal;
	private bool useTangent;
	private bool useUV;
	private bool useUV2;

	private List<MeshVertex> vertices = new List<MeshVertex>();
	private List<List<int>> triangles = new List<List<int>>();
	private List<int> currentTriangleList;

	private int startFaceVertexCount = 0;
	private int startFaceIndexCount = 0;

	private readonly float equalityToleranceSqrd = 0.0001f;

	public MeshBuilder(bool usePosition, bool useNormal, bool useTangent, bool useUV, bool useUV2)
	{
		this.usePosition = usePosition;
		this.useNormal = useNormal;
		this.useTangent = useTangent;
		this.useUV = useUV;
		this.useUV2 = useUV2;
	}

	public void BeginSubMesh()
	{
		currentTriangleList = new List<int>();
		triangles.Add(currentTriangleList);
	}

	public void BeginFace()
	{
		startFaceVertexCount = vertices.Count;
		startFaceIndexCount = currentTriangleList.Count;
	}

	private int GetVertexIndex(MeshVertex vertex)
	{
		for (int i = 0; i < vertices.Count; ++i)
		{
			MeshVertex existingVertex = vertices[i];

			if ((!usePosition || (vertex.position - existingVertex.position).sqrMagnitude < equalityToleranceSqrd) &&
			    (!useNormal || (vertex.normal - existingVertex.normal).sqrMagnitude < equalityToleranceSqrd) &&
				(!useTangent || (vertex.tangent - existingVertex.tangent).sqrMagnitude < equalityToleranceSqrd) &&
				(!useUV || (vertex.uv - existingVertex.uv).sqrMagnitude < equalityToleranceSqrd) &&
				(!useUV2 || (vertex.uv2 - existingVertex.uv2).sqrMagnitude < equalityToleranceSqrd))
			{
				return i;
			}
		}

		vertices.Add(vertex);
		return vertices.Count - 1;
	}

	public void AddVertex(MeshVertex vertex)
	{
		int vertexCount = currentTriangleList.Count - startFaceIndexCount;
		int index = GetVertexIndex(vertex);

		if (vertexCount > 1 && 
		    (index == currentTriangleList[currentTriangleList.Count - 1] || 
			index == currentTriangleList[startFaceIndexCount]))
		{
			// duplicate vertex, do no add
			return;
		}

		if (vertexCount >= 3)
		{
			// triangulate the face in a triangle fan
			int previousIndex = currentTriangleList[currentTriangleList.Count - 1];
			currentTriangleList.Add(currentTriangleList[startFaceIndexCount]);
			currentTriangleList.Add(previousIndex);
		}

		currentTriangleList.Add(index);
	}

	public void EndFace()
	{
		int vertexCount = currentTriangleList.Count - startFaceIndexCount;

		if (vertexCount < 3)
		{
			// remove vertices for the incomplete face
			currentTriangleList.RemoveRange(startFaceIndexCount, vertexCount);
			vertices.RemoveRange(startFaceVertexCount, vertices.Count - startFaceVertexCount);
		}
	}

	public void EndSubMesh()
	{

	}

	public Mesh BuildMesh(Mesh existingMesh = null)
	{
		Mesh result = existingMesh ?? new Mesh();
		Vector3[] positions = usePosition ? new Vector3[vertices.Count] : null;
		Vector3[] normals = useNormal ? new Vector3[vertices.Count] : null;
		Vector4[] tangents = useTangent ? new Vector4[vertices.Count] : null;
		Vector2[] uv = useUV ? new Vector2[vertices.Count] : null;
		Vector2[] uv2 = useUV2 ? new Vector2[vertices.Count] : null;

		for (int i = 0; i < vertices.Count; ++i)
		{
			if (usePosition) positions[i] = vertices[i].position;
			if (useNormal) normals[i] = vertices[i].normal;
			if (useTangent) tangents[i] = vertices[i].tangent;
			if (useUV) uv[i] = vertices[i].uv;
			if (useUV2) uv2[i] = vertices[i].uv2;
		}

		result.vertices = positions;
		result.normals = normals;
		result.tangents = tangents;
		result.uv = uv;
		result.uv2 = uv2;

		result.subMeshCount = triangles.Count;

		for (int i = 0; i < triangles.Count; ++i)
		{
			result.SetTriangles(triangles[i].ToArray(), i);
		}

		result.Optimize();
		result.RecalculateBounds();

		return result;
	}
}




public struct MeshTriangleAddress
{
	public MeshTriangleAddress(int submesh, int indexOffset)
	{
		this.submesh = submesh;
		this.indexOffset = indexOffset;
	}

	public int submesh;
	public int indexOffset;

	public static bool operator==(MeshTriangleAddress a, MeshTriangleAddress b)
	{
		return a.submesh == b.submesh && a.indexOffset == b.indexOffset;
	}

	public static bool operator!=(MeshTriangleAddress a, MeshTriangleAddress b)
	{
		return !(a == b);
	}

	public override bool Equals (object obj)
	{
		return obj is MeshTriangleAddress && this == (MeshTriangleAddress)obj;
	}
	
	public override int GetHashCode ()
	{
		return submesh.GetHashCode() ^ indexOffset.GetHashCode();
	}
}

public class MeshTriangleEdge
{
	private List<MeshTriangleAddress> triangleAddresses;

	public MeshTriangleEdge(MeshTriangleAddress triangleAddress)
	{
		triangleAddresses = new List<MeshTriangleAddress>();
		triangleAddresses.Add(triangleAddress);
	}

	public void AddTriangle(MeshTriangleAddress triangleAddress)
	{
		triangleAddresses.Add(triangleAddress);
	}

	public List<MeshTriangleAddress> GetAdjacentTriangles()
	{
		return triangleAddresses;
	}
}

public class MeshSimplifier
{
	private Dictionary<int, MeshTriangleEdge> edgeConnection = new Dictionary<int, MeshTriangleEdge>();
	private List<List<MeshTriangleAddress>> groupToTriangles = new List<List<MeshTriangleAddress>>();
	private Dictionary<MeshTriangleAddress, int> trianglesToGroup = new Dictionary<MeshTriangleAddress, int>();
	private List<MeshTriangleAddress> currentGroup = null;
	
	private List<GroupEdgeLoopBuilder> groupEdgeLoops = new List<GroupEdgeLoopBuilder>();

	private Mesh mesh;
	
	private Vector3[] positions;
	private Vector3[] normals;
	private Vector4[] tangents;
	private Vector2[] uv;
	private Vector2[] uv2;

	private bool[] isVertexNeeded;
	
	private List<int[]> indices = new List<int[]>();
	
	private bool useNormal;
	private bool useTangent;
	private bool useUV;
	private bool useUV2;
	
	private static readonly float POSITION_COPLANAR_TOLERANCE = 0.0001f;
	private static readonly float UV_COPLANAR_TOLERANCE = 0.0001f;
	private static readonly float EDGE_OVERLAP_TOLERANCE = 0.001f;

	private Dictionary<int, int> indexMapping = new Dictionary<int, int>();

	private class GroupEdgeLoopBuilder
	{
		private List<List<int>> edgeLoops = new List<List<int>>();
		private Vector3 normal;
		private int submesh;

		public GroupEdgeLoopBuilder(Vector3 normal, int submesh)
		{
			this.normal = normal;
			this.submesh = submesh;
		}

		public int Submesh
		{
			get
			{
				return submesh;
			}
		}

		public void AddEdge(int vertexA, int vertexB)
		{
			List<int> sourceLoop = null;
			List<int> targetLoop = null;

			foreach (List<int> existingLoop in edgeLoops)
			{
				if (existingLoop[existingLoop.Count - 1] == vertexA)
				{
					sourceLoop = existingLoop;
				}

				if (existingLoop[0] == vertexB)
				{
					targetLoop = existingLoop;
				}
			}

			if (sourceLoop == null && targetLoop == null)
			{
				List<int> newLoop = new List<int>();
				newLoop.Add(vertexA);
				newLoop.Add(vertexB);
				edgeLoops.Add(newLoop);
			}
			else if (targetLoop == null)
			{
				sourceLoop.Add(vertexB);
			}
			else if (sourceLoop == null)
			{
				targetLoop.Insert(0, vertexA);
			}
			else
			{
				if (targetLoop == sourceLoop)
				{
					// the loop has been closed
					// place the same vertex as the start
					// and end of the edge loop
					// the extra value will be removed
					// after all edges are found
					targetLoop.Add(vertexB);
				}
				else
				{
					// join two loops
					sourceLoop.AddRange(targetLoop);
					edgeLoops.Remove(targetLoop);
				}
			}
		}

		public void RemoveExtraLoopEnds()
		{
			foreach (List<int> loop in edgeLoops)
			{
				if (loop[0] != loop[loop.Count - 1])
				{
					Debug.LogError("Edge loop did not close");
				}
				else
				{
					loop.RemoveAt(loop.Count - 1);
				}
			}
		}

		private static bool IsColinear(Vector3 a, Vector3 b, Vector3 c)
		{
			Vector3 reprojectedPoint = Vector3.Project(b - a, c - a) + a;
			return (reprojectedPoint - b).sqrMagnitude < POSITION_COPLANAR_TOLERANCE;
		}

		public void MarkNeededVertices(bool[] isVertexNeeded, Vector3[] positions)
		{
			foreach (List<int> loop in edgeLoops)
			{
				for (int index = 0; index < loop.Count; ++ index)
				{
					int vertexIndex = loop[index];
					int nextIndex = loop[(index + 1) % loop.Count];
					int nextNextIndex = loop[(index + 2) % loop.Count];

					if (!IsColinear(positions[vertexIndex], positions[nextIndex], positions[nextNextIndex]))
					{
						isVertexNeeded[nextIndex] = true;
					}
				}
			}
		}

		public void RemoveUneededVertices(bool[] isVertexNeeded)
		{
			foreach (List<int> loop in edgeLoops)
			{
				for (int index = 0; index < loop.Count;)
				{
					if (isVertexNeeded[loop[index]])
					{
						++index;
					}
					else
					{
						loop.RemoveAt(index);
					}
				}
			}
		}

		// connects hole edge loops into the outer edge loop
		// resulting in a single edge loop. See the ascii art
		// below for more description
		//
		// -----------
		// |         |
		// |  ---    |
		// |  | | ^  | |
		// |  --- |  | v
		// |         |
		// -----------
		//  the outer loop has a clockwise winding
		//  the inner loop is a hole in the outer loop
		//  after combining loops they will be joined together
		//
		// -----------
		// |\        |
		// | \---    |
		// |  | | ^  | |
		// |  --- |  | v
		// |         |
		// -----------
		// the connection between the two loops is two way
		// if you follow the winding of the outer loop it is
		// consistent with the winding of the inner loop and
		// once all loops are joined the remaining loop is 
		// ready to be retriangulated

		public void CombineLoops(Vector3[] positions)
		{
			while (edgeLoops.Count > 1)
			{
				int i = 0;

				for (i = 1; i < edgeLoops.Count; ++i)
				{
					if (AttemptCombine(i, positions))
					{
						i = 0;
						break;
					}
				}

				if (i == edgeLoops.Count)
				{
					Debug.LogError("Could not connect edge loops");
					break;
				}
			}
		}

		public bool IsTriangleEmpty(int vertexA, int vertexB, int vertexC, Vector3[] positions)
		{
			Vector3 positionA = positions[vertexA];
			Vector3 positionB = positions[vertexB];
			Vector3 positionC = positions[vertexC];

			Vector3 edgeA = positionB - positionA;
			Vector3 edgeB = positionC - positionB;
			Vector3 edgeC = positionA - positionC;
			
			foreach (List<int> loop in edgeLoops)
			{
				for (int index = 0; index < loop.Count; ++index)
				{
					int otherVertex = loop[index];

					if (otherVertex == vertexA || otherVertex == vertexB || otherVertex == vertexC)
					{
						continue;
					}

					Vector3 positionCheck = positions[otherVertex];

					if (Vector3.Dot(normal, Vector3.Cross(edgeA, positionCheck - positionA)) >= -EDGE_OVERLAP_TOLERANCE &&
					    Vector3.Dot(normal, Vector3.Cross(edgeB, positionCheck - positionB)) >= -EDGE_OVERLAP_TOLERANCE &&
					    Vector3.Dot(normal, Vector3.Cross(edgeC, positionCheck - positionC)) >= -EDGE_OVERLAP_TOLERANCE)
					{
						return false;
					}
				}
			}

			return true;
		}

		public bool IsEdgeClear(int vertexA, int vertexB, Vector3[] positions)
		{
			Vector3 positionA = positions[vertexA];
			Vector3 positionB = positions[vertexB];

			Vector3 direction = positionB - positionA;

			Ray edgeRay = new Ray(positionA, direction);
			float edgeLength = Vector3.Dot(edgeRay.direction, direction);

			foreach (List<int> loop in edgeLoops)
			{
				for (int index = 0; index < loop.Count; ++index)
				{
					int otherVertexA = loop[index];
					int otherVertexB = loop[(index + 1) % loop.Count];

					if (otherVertexA == vertexA || otherVertexA == vertexB ||
					    otherVertexB == vertexA || otherVertexB == vertexB)
					{
						continue;
					}

					Vector3 loopPositionA = positions[otherVertexA];
					Vector3 loopPositionB = positions[otherVertexB];

					float distanceAlongEdge = MeshTools.GetDistanceOfNearsestPoint(edgeRay, new Ray(loopPositionA, loopPositionB - loopPositionA));

					Vector3 segmentDirection = loopPositionB - loopPositionA;

					if (segmentDirection.sqrMagnitude == 0.0f)
					{
						continue;
					}

					float otherLength = segmentDirection.magnitude;

					float otherDistance = Vector3.Dot(edgeRay.GetPoint(distanceAlongEdge) - loopPositionA, segmentDirection) / otherLength;

					if (distanceAlongEdge > -EDGE_OVERLAP_TOLERANCE && distanceAlongEdge < edgeLength + EDGE_OVERLAP_TOLERANCE &&
					    otherDistance > -EDGE_OVERLAP_TOLERANCE && otherDistance < otherLength + EDGE_OVERLAP_TOLERANCE)
					{
						// edge overlap detected
						return false;
					}
				}
			}

			return true;
		}

		public bool AttemptCombine(int otherIndex, Vector3[] positions)
		{
			List<int> loopA = edgeLoops[0];
			List<int> loopB = edgeLoops[otherIndex];

			for (int loopAIndex = 0; loopAIndex < loopA.Count; ++loopAIndex)
			{
				Vector3 lastPositionA = positions[loopA[(loopAIndex + loopA.Count - 1) % loopA.Count]];
				Vector3 positionA = positions[loopA[loopAIndex]];
				Vector3 nextPositionA = positions[loopA[(loopAIndex + 1) % loopA.Count]];

				for (int loopBIndex = 0; loopBIndex < loopB.Count; ++loopBIndex)
				{
					Vector3 positionB = positions[loopB[loopBIndex]];

					if (IsEdgeClear(loopA[loopAIndex], loopB[loopBIndex], positions) &&
					    Vector3.Dot(normal, Vector3.Cross(positionA - lastPositionA, positionB - positionA)) > 0.0f &&
					    Vector3.Dot(normal, Vector3.Cross(positionA - positionB, nextPositionA - positionA)) > 0.0f)
					{
						loopA.Insert(loopAIndex + 1, loopA[loopAIndex]);
						loopA.Insert(loopAIndex + 1, loopB[loopBIndex]);
						loopA.InsertRange(loopAIndex + 1, loopB.GetRange(0, loopBIndex));
						loopA.InsertRange(loopAIndex + 1, loopB.GetRange(loopBIndex, loopB.Count - loopBIndex));

						edgeLoops.Remove(loopB);

						return true;
					}
				}
			}

			return false;
		}

		public void Triangulate(List<int> output, Dictionary<int, int> indexMapping, Vector3[] positions)
		{
			List<int> edgeLoop = edgeLoops[0];

			int failCount = 0;
			int currentVertex = 0;

			while (edgeLoop.Count > 2)
			{
				int indexA = edgeLoop[currentVertex];
				int indexB = edgeLoop[(currentVertex + 1) % edgeLoop.Count];
				int indexC = edgeLoop[(currentVertex + 2) % edgeLoop.Count];

				Vector3 vertexA = positions[indexA];
				Vector3 vertexB = positions[indexB];
				Vector3 vertexC = positions[indexC];

				Vector3 faceNormal = Vector3.Cross(vertexB - vertexA, vertexC - vertexA);

				if (Vector3.Dot(faceNormal, normal) > 0.0f && 
				    IsEdgeClear(indexA, indexB, positions) && 
				    IsEdgeClear(indexB, indexC, positions) && 
				    IsTriangleEmpty(indexA, indexB, indexC, positions))
				{
					output.Add(indexMapping[indexA]);
					output.Add(indexMapping[indexB]);
					output.Add(indexMapping[indexC]);

					edgeLoop.RemoveAt((currentVertex + 1) % edgeLoop.Count);
					currentVertex = (currentVertex + 1) % edgeLoop.Count;
					failCount = 0;
				}
				else
				{
					currentVertex = (currentVertex + 1) % edgeLoop.Count;
					++failCount;

					if (failCount > edgeLoop.Count * 2)
					{
						Debug.LogError("Infinite loop detected");
						break;
					}
				}
			}
		}
	}

	public MeshSimplifier(Mesh mesh, bool positionOnly = false)
	{
		this.mesh = mesh;
		
		positions = mesh.vertices;
		normals = positionOnly ? null : mesh.normals;
		tangents = positionOnly ? null : mesh.tangents;
		uv = positionOnly ? null : mesh.uv;
		uv2 = positionOnly ? null : mesh.uv2;

		isVertexNeeded = new bool[positions.Length];

		useNormal = normals != null && normals.Length > 0;
		useTangent = tangents != null && tangents.Length > 0;
		useUV = uv != null && uv.Length > 0;
		useUV2 = uv2 != null && uv2.Length > 0;

		for (int i = 0; i < mesh.subMeshCount; ++ i)
		{
			indices.Add(mesh.GetTriangles(i));
		}
	}

	public Mesh Simplify(Mesh target = null)
	{
		Mesh result = target ?? new Mesh();

		// find which faces are connected to which edges
		BuildEdgeConnections();
		// traverse all the faces putting all connected
		// coplanar faces into groups
		BuildGroups();
		// loop over each group and determine the edges
		// where it meets with adjacent groups
		BuildEdgeLoops();
		// mark any vertices that are needed.
		// a vertex is needed if it is a corner
		// on any of the edge loops
		MarkNeededVertices();
		// remove all uneeded vertices from the edge loops
		// and combines all loops
		RemoveUneededVertices();

		BuildMesh(result);

		return result;
	}

	private void BuildEdgeLoops()
	{
		Dictionary<int, int> groupOccurenceCount = new Dictionary<int, int>();

		foreach (KeyValuePair<int, MeshTriangleEdge> edge in edgeConnection)
		{
			int vertexA = VertexAFromEdge(edge.Key);
			int vertexB = VertexBFromEdge(edge.Key);

			groupOccurenceCount.Clear();

			// determine the number of faces from each group meets at the given edge
			foreach (MeshTriangleAddress address in edge.Value.GetAdjacentTriangles())
			{
				int group = trianglesToGroup[address];

				if (groupOccurenceCount.ContainsKey(group))
				{
					groupOccurenceCount[group] = groupOccurenceCount[group] + 1;
				}
				else
				{
					groupOccurenceCount[group] = 1;
				}
			}

			foreach (MeshTriangleAddress address in edge.Value.GetAdjacentTriangles())
			{
				int group = trianglesToGroup[address];

				// check to see of the group only shows up once
				// if so, then mark it as an edge
				if (groupOccurenceCount[group] == 1)
				{
					bool shouldFlipOrder = true;

					// determine the direction of the edge
					for (int i = 0; i < 3; ++i)
					{
						int vertexIndex = VertexIndex(address, i);
						int nextIndex = VertexIndex(address, (i + 1) % 3);

						if (vertexA == vertexIndex && vertexB == nextIndex)
						{
							shouldFlipOrder = false;
							break;
						}
					}

					if (shouldFlipOrder)
					{
						groupEdgeLoops[group].AddEdge(vertexB, vertexA);
					}
					else
					{
						groupEdgeLoops[group].AddEdge(vertexA, vertexB);
					}
				}
			}
		}
	}

	private void MarkNeededVertices()
	{
		foreach (GroupEdgeLoopBuilder groupBuilder in groupEdgeLoops)
		{
			groupBuilder.RemoveExtraLoopEnds();
			groupBuilder.MarkNeededVertices(isVertexNeeded, positions);
		}
		
		int newIndex = 0;
		
		for (int lastIndex = 0; lastIndex < isVertexNeeded.Length; ++lastIndex)
		{
			if (isVertexNeeded[lastIndex])
			{
				indexMapping[lastIndex] = newIndex;
				++newIndex;
			}
		}
	}
	
	private void RemoveUneededVertices()
	{
		foreach (GroupEdgeLoopBuilder groupBuilder in groupEdgeLoops)
		{
			groupBuilder.RemoveUneededVertices(isVertexNeeded);
			groupBuilder.CombineLoops(positions);
		}
	}

	private void BuildMesh(Mesh targetMesh)
	{
		int newVertexCount = indexMapping.Count;

		Vector3[] newPositions = new Vector3[newVertexCount];
		Vector3[] newNormals = useNormal ? new Vector3[newVertexCount] : new Vector3[0];
		Vector4[] newTangnets = useTangent ? new Vector4[newVertexCount] : new Vector4[0];
		Vector2[] newUV = useUV ? new Vector2[newVertexCount] : new Vector2[0];
		Vector2[] newUV2 = useUV2 ? new Vector2[newVertexCount] : new Vector2[0];

		int newIndex = 0;

		for (int i = 0; i < positions.Length; ++i)
		{
			if (isVertexNeeded[i])
			{
				newPositions[newIndex] = positions[i];

				if (useNormal) newNormals[newIndex] = normals[i];
				if (useTangent) newTangnets[newIndex] = tangents[i];
				if (useUV) newUV[newIndex] = uv[i];
				if (useUV2) newUV2[newIndex] = uv2[i];

				++newIndex;
			}
		}

		List<List<int>> newIndices = new List<List<int>>();

		for (int i = 0; i < indices.Count; ++i)
		{
			newIndices.Add(new List<int>());
		}

		foreach (GroupEdgeLoopBuilder groupEdge in groupEdgeLoops)
		{
			groupEdge.Triangulate(newIndices[groupEdge.Submesh], indexMapping, positions);
		}

		targetMesh.subMeshCount = newIndices.Count;
		
		for (int i = 0; i < newIndices.Count; ++i)
		{
			targetMesh.SetTriangles(newIndices[i].ToArray(), i);
		}
		
		targetMesh.vertices = newPositions;
		targetMesh.normals = newNormals;
		targetMesh.tangents = newTangnets;
		targetMesh.uv = newUV;
		targetMesh.uv2 = newUV2;
	}

	private List<MeshTriangleAddress> GetAdjacentFaces(int vertexA, int vertexB)
	{
		int edgeIndex = EdgeIndex(vertexA, vertexB);

		if (edgeConnection.ContainsKey(edgeIndex))
		{
			return edgeConnection[edgeIndex].GetAdjacentTriangles();
		}
		else
		{
			return null;
		}
	}

	private static int EdgeIndex(int vertexA, int vertexB)
	{
		if (vertexA < vertexB)
		{
			return (vertexA << 16) | vertexB;
		}
		else
		{
			return (vertexB << 16) | vertexA;
		}
	}

	private static int VertexAFromEdge(int edgeIndex)
	{
		return edgeIndex >> 16;
	}

	private static int VertexBFromEdge(int edgeIndex)
	{
		return edgeIndex & 0xFFFF;
	}

	private void AddTriangleToEdge(int vertexA, int vertexB, MeshTriangleAddress triangleAddress)
	{
		int edgeIndex = EdgeIndex(vertexA, vertexB);

		if (edgeConnection.ContainsKey(edgeIndex))
		{
			edgeConnection[edgeIndex].AddTriangle(triangleAddress);
		}
		else
		{
			edgeConnection[edgeIndex] = new MeshTriangleEdge(triangleAddress);
		}
	}

	private void BuildEdgeConnections()
	{
		for (int submesh = 0; submesh < mesh.subMeshCount; ++submesh)
		{
			int[] indices = mesh.GetTriangles(submesh);

			for (int index = 0; index + 2 < indices.Length; index += 3)
			{
				MeshTriangleAddress triangleAddress = new MeshTriangleAddress(submesh, index);

				for (int vertex = 0; vertex < 3; ++vertex)
				{
					AddTriangleToEdge(indices[index + vertex], indices[index + (vertex + 1) % 3], triangleAddress);
				}
			}
		}
	}

	private int CurrentGroupIndex
	{
		get
		{
			return groupToTriangles.Count;
		}
	}

	private Vector3 BarycentricCoords(Vector3 a, Vector3 b, Vector3 c, Vector3 samplePoint)
	{
		Vector3 normal = Vector3.Cross(b - a, c - a);
		Vector3 projectedPoint = samplePoint - Vector3.Project((samplePoint - a), normal);

		float triangleAreaSqrd = normal.sqrMagnitude;

		return new Vector3(
			Vector3.Dot(Vector3.Cross(c - b, projectedPoint - b), normal) / triangleAreaSqrd,
			Vector3.Dot(Vector3.Cross(a - c, projectedPoint - c), normal) / triangleAreaSqrd,
			Vector3.Dot(Vector3.Cross(b - a, projectedPoint - a), normal) / triangleAreaSqrd);
	}

	private Vector3 BarycentricLerp(Vector3 a, Vector3 b, Vector3 c, Vector3 barycentricCoord)
	{
		return a * barycentricCoord.x + b * barycentricCoord.y + c * barycentricCoord.z;
	}
	
	private Vector2 BarycentricLerp(Vector2 a, Vector2 b, Vector2 c, Vector3 barycentricCoord)
	{
		return a * barycentricCoord.x + b * barycentricCoord.y + c * barycentricCoord.z;
	}

	private bool IsCoplanar(MeshTriangleAddress triangleA, MeshTriangleAddress triangleB)
	{
		if (triangleA.submesh != triangleB.submesh)
		{
			return false;
		}

		int indexA0 = VertexIndex(triangleA, 0);
		int indexA1 = VertexIndex(triangleA, 1);
		int indexA2 = VertexIndex(triangleA, 2);

		Vector3 positionA0 = positions[indexA0];
		Vector3 positionA1 = positions[indexA1];
		Vector3 positionA2 = positions[indexA2];

		for (int i = 0; i < 3; ++i)
		{
			int indexB = VertexIndex(triangleB, i);
			Vector3 positionB = positions[indexB];

			Vector3 barycentricCoord = BarycentricCoords(positionA0, positionA1, positionA2, positionB);

			Vector3 positionBPrediction = BarycentricLerp(positionA0, positionA1, positionA2, barycentricCoord);

			if ((positionB - positionBPrediction).sqrMagnitude > POSITION_COPLANAR_TOLERANCE)
			{
				return false;
			}

			if (useUV)
			{
				Vector2 uvB = uv[indexB];
				Vector2 predictedUV = BarycentricLerp(uv[indexA0], uv[indexA1], uv[indexA2], barycentricCoord);

				if ((uvB - predictedUV).sqrMagnitude > UV_COPLANAR_TOLERANCE)
				{
					return false;
				}
			}

			if (useUV2)
			{
				Vector2 uvB = uv2[indexB];
				Vector2 predictedUV = BarycentricLerp(uv2[indexA0], uv2[indexA1], uv2[indexA2], barycentricCoord);
				
				if ((uvB - predictedUV).sqrMagnitude > UV_COPLANAR_TOLERANCE)
				{
					return false;
				}
			}

			// if the positions and uvs are coplanar then the normal and tangent are 
			// assumed to be coplanar so they aren't checked
		}
		
		Vector3 positionB0 = positions[VertexIndex(triangleB, 0)];
		Vector3 positionB1 = positions[VertexIndex(triangleB, 1)];
		Vector3 positionB2 = positions[VertexIndex(triangleB, 2)];

		if (Vector3.Dot(Vector3.Cross(positionB1 - positionB0, positionB2 - positionB0), 
		                Vector3.Cross(positionA1 - positionA0, positionA2 - positionA0)) < 0.0f)
		{
			return false;
		}

		return true;
	}

	private int VertexIndex(MeshTriangleAddress address, int vertexOffset)
	{
		return indices[address.submesh][address.indexOffset + vertexOffset];
	}

	private void BuildGroup(MeshTriangleAddress address)
	{
		currentGroup.Add(address);
		trianglesToGroup[address] = CurrentGroupIndex;

		for (int i = 0; i < 3; ++i)
		{
			int vertexA = VertexIndex(address, i);
			int vertexB = VertexIndex(address, (i + 1) % 3);

			List<MeshTriangleAddress> adjacentEdges = GetAdjacentFaces(vertexA, vertexB);

			// only follow edges where two faces meet
			if (adjacentEdges.Count == 2)
			{
				foreach (MeshTriangleAddress otherAddress in adjacentEdges)
				{
					if (otherAddress != address && !trianglesToGroup.ContainsKey(otherAddress) && IsCoplanar(address, otherAddress))
					{
						BuildGroup(otherAddress);
					}
				}
			}
		}
	}

	private Vector3 TriangleNormal(MeshTriangleAddress address)
	{
		Vector3 a = positions[VertexIndex(address, 0)];
		Vector3 b = positions[VertexIndex(address, 1)];
		Vector3 c = positions[VertexIndex(address, 2)];

		return Vector3.Cross(b - a, c - a).normalized;
	}

	private void BuildGroups()
	{
		for (int submesh = 0; submesh < mesh.subMeshCount; ++submesh)
		{
			int[] indices = mesh.GetTriangles(submesh);
			
			for (int index = 0; index + 2 < indices.Length; index += 3)
			{
				MeshTriangleAddress triangleAddress = new MeshTriangleAddress(submesh, index);

				if (!trianglesToGroup.ContainsKey(triangleAddress))
				{
					currentGroup = new List<MeshTriangleAddress>();

					BuildGroup(triangleAddress);

					groupToTriangles.Add(currentGroup);
					groupEdgeLoops.Add(new GroupEdgeLoopBuilder(TriangleNormal(triangleAddress), triangleAddress.submesh));
				}
			}
		}
	}
}

public static class MeshTools {

	private static MeshVertex GetVertex(int index, Vector3[] positions, Vector3[] normals, Vector4[] tangents, Vector2[] uv, Vector2[] uv2)
	{
		return new MeshVertex(
			(positions == null || index >= positions.Length) ? Vector3.zero : positions[index],
			(normals == null || index >= normals.Length) ? Vector3.zero : normals[index],
			(tangents == null || index >= tangents.Length) ? Vector4.zero : tangents[index],
			(uv == null || index >= uv.Length) ? Vector2.zero : uv[index],
			(uv2 == null || index >= uv2.Length) ? Vector2.zero : uv2[index]);
	}

	public static Mesh[] Split(this Mesh mesh, Plane splitPlane)
	{
		Vector3[] positions = mesh.vertices;
		Vector3[] normals = mesh.normals;
		Vector4[] tangents = mesh.tangents;
		Vector2[] uv = mesh.uv;
		Vector2[] uv2 = mesh.uv2;

		bool usePosition = positions != null && positions.Length > 0;
		bool useNormal = normals != null && normals.Length > 0;
		bool useTangent = tangents != null && tangents.Length > 0;
		bool useUV = uv != null && uv.Length > 0;
		bool useUV2 = uv2 != null && uv2.Length > 0;

		MeshBuilder sideA = new MeshBuilder(usePosition, useNormal, useTangent, useUV, useUV2);
		MeshBuilder sideB = new MeshBuilder(usePosition, useNormal, useTangent, useUV, useUV2);

		bool[] onSideA = new bool[3];
		bool[] onSideB = new bool[3];
		float[] pointDistance = new float[3];
		MeshVertex[] vertices = new MeshVertex[3];

		for (int submesh = 0; submesh < mesh.subMeshCount; ++submesh)
		{
			int[] triangles = mesh.GetTriangles(submesh);

			sideA.BeginSubMesh();
			sideB.BeginSubMesh();

			for (int index = 0; index + 2 < triangles.Length; index += 3)
			{
				bool allOnSideA = true;
				bool allOnSideB = true;

				for (int vertex = 0; vertex < 3; ++vertex)
				{
					int vertexIndex = triangles[index + vertex];

					pointDistance[vertex] = splitPlane.GetDistanceToPoint(positions[vertexIndex]);

					onSideA[vertex] = pointDistance[vertex] >= 0.0f;
					onSideB[vertex] = pointDistance[vertex] <= 0.0f;

					allOnSideA = allOnSideA && onSideA[vertex];
					allOnSideB = allOnSideB && onSideB[vertex];

					vertices[vertex] = GetVertex(vertexIndex, positions, normals, tangents, uv, uv2);
				}

				if (allOnSideA)
				{
					sideA.BeginFace();
					foreach (MeshVertex vertex in vertices)
					{
						sideA.AddVertex(vertex);
					}
					sideA.EndFace();
				}
				else if (allOnSideB)
				{
					sideB.BeginFace();
					foreach (MeshVertex vertex in vertices)
					{
						sideB.AddVertex(vertex);
					}
					sideB.EndFace();
				}
				else
				{
					sideA.BeginFace();
					sideB.BeginFace();
					for (int i = 0; i < 3; ++i)
					{
						if (onSideA[i]) sideA.AddVertex(vertices[i]);
						if (onSideB[i]) sideB.AddVertex(vertices[i]);

						int nextVertex = (i + 1) % 3;

						// determine if the edge crosses the plane
						if (onSideA[i] != onSideB[i] && 
						    onSideA[nextVertex] != onSideB[nextVertex] &&
						    onSideA[i] != onSideA[nextVertex])
						{
							float t = -pointDistance[i] / (pointDistance[nextVertex] - pointDistance[i]);
							MeshVertex lerped = MeshVertex.Lerp(vertices[i], vertices[nextVertex], t);
							sideA.AddVertex(lerped);
							sideB.AddVertex(lerped);
						}
					}
					sideA.EndFace();
					sideB.EndFace();
				}
			}

			sideA.EndSubMesh();
			sideB.EndSubMesh();
		}

		return new Mesh[]{sideA.BuildMesh(), sideB.BuildMesh()};
	}

	public static void ShiftVertices(this Mesh mesh, Vector3 direction)
	{
		Vector3[] vertices = mesh.vertices;

		for (int i = 0; i < vertices.Length; ++i)
		{
			vertices[i] += direction;
		}

		mesh.vertices = vertices;

		mesh.RecalculateBounds();
	}

	public static int GetMaxInstanceCount(List<CombineInstance> instanceList)
	{
		int currentVertexCount = 0;
		int result = 0;

		while (result < instanceList.Count)
		{
			if (instanceList[result].mesh != null)
			{
				currentVertexCount += instanceList[result].mesh.vertexCount;
				
				// max vertex count
				if (currentVertexCount > 0x10000)
				{
					return result;
				}
			}

			++result;
		}

		return result;
	}

	public static Mesh RemoveDuplicateVertices(this Mesh mesh, bool positionDataOnly = false)
	{
		Vector3[] positions = mesh.vertices;
		Vector3[] normals = positionDataOnly ? null : mesh.normals;
		Vector4[] tangents = positionDataOnly ? null : mesh.tangents;
		Vector2[] uv = positionDataOnly ? null : mesh.uv;
		Vector2[] uv2 = positionDataOnly ? null : mesh.uv2;
		
		bool usePosition = positions != null && positions.Length > 0;
		bool useNormal = normals != null && normals.Length > 0;
		bool useTangent = tangents != null && tangents.Length > 0;
		bool useUV = uv != null && uv.Length > 0;
		bool useUV2 = uv2 != null && uv2.Length > 0;
		
		MeshBuilder meshBuilder = new MeshBuilder(usePosition, useNormal, useTangent, useUV, useUV2);

		for (int submesh = 0; submesh < mesh.subMeshCount; ++submesh)
		{
			meshBuilder.BeginSubMesh();

			int[] indices = mesh.GetTriangles(submesh);

			for (int index = 0; index < indices.Length; ++index)
			{
				if (index % 3 == 0)
				{
					meshBuilder.BeginFace();
				}

				int vertexIndex = indices[index];

				meshBuilder.AddVertex(new MeshVertex(
					usePosition ? positions[vertexIndex] : Vector3.zero,
					useNormal ? normals[vertexIndex] : Vector3.zero,
					useTangent ? tangents[vertexIndex] : Vector4.zero,
					useUV ? uv[vertexIndex] : Vector2.zero,
					useUV2 ? uv2[vertexIndex] : Vector2.zero));

				if (index % 3 == 2 || index == indices.Length - 1)
				{
					meshBuilder.EndFace();
				}
			}

			meshBuilder.EndSubMesh();
		}

		return meshBuilder.BuildMesh();
	}

	public static Mesh Simplify(this Mesh mesh, bool positionDataOnly = false)
	{
		Mesh result = RemoveDuplicateVertices(mesh, positionDataOnly);

		MeshSimplifier simplifier = new MeshSimplifier(result, positionDataOnly);
		simplifier.Simplify(result);

		result.name = mesh.name + " simplified";

		return result;
	}
	
	public static float GetDistanceOfNearsestPoint(Ray onRay, Ray fromRay)
	{
		float denom = 1.0f - Mathf.Pow(Vector3.Dot(onRay.direction, fromRay.direction), 2.0f);
		
		if (denom < 0.00001f)
		{
			return float.PositiveInfinity;
		}
		else
		{
			Vector3 projectedOnFrom = fromRay.origin + fromRay.direction * Vector3.Dot(onRay.origin - fromRay.origin, fromRay.direction);
			return Vector3.Dot(onRay.direction, projectedOnFrom - onRay.origin) / denom;
		}
	}

	// used to get the warning to go away that the mesh wants texture coordinates
	public static void AddEmptyTextureCoordinates(this Mesh mesh)
	{
		mesh.uv = new Vector2[mesh.vertexCount];
	}
}
