using UnityEngine;
using System.Collections;

public class AimIndicator : MonoBehaviour {

	public Material aimMaterial;

	private static readonly int DEFAULT_STRIP_RESOLUTION = 16;

	private static Mesh BuildMesh(int resolution)
	{
		Mesh result = new Mesh();

		int pointCount = resolution * 2;

		Vector3[] vertexPositions = new Vector3[pointCount];

		int currentIndex = 0;
		float xPosition = 0.0f;
		float xStep = 1.0f / (resolution - 1);

		while (currentIndex < pointCount)
		{
			vertexPositions[currentIndex + 0] = new Vector3(xPosition, 1.0f, 0.0f);
			vertexPositions[currentIndex + 1] = new Vector3(xPosition, -1.0f, 0.0f);

			currentIndex += 2;
			xPosition += xStep;
		}

		int indexCount = (resolution - 1) * 6;
		int[] indices = new int[indexCount];

		currentIndex = 0;
		int vertexOffset = 0;
		while (currentIndex < indexCount)
		{
			indices[currentIndex + 0] = vertexOffset + 0;
			indices[currentIndex + 1] = vertexOffset + 2;
			indices[currentIndex + 2] = vertexOffset + 1;
			indices[currentIndex + 3] = vertexOffset + 1;
			indices[currentIndex + 4] = vertexOffset + 2;
			indices[currentIndex + 5] = vertexOffset + 3;

			currentIndex += 6;
			vertexOffset += 2;
		}

		result.subMeshCount = 1;

		result.vertices = vertexPositions;
		result.SetIndices(indices, MeshTopology.Triangles, 0);

		result.Optimize();

		return result;
	}

	private static Mesh stripMesh = null;

	public static Mesh GetStripMesh()
	{
		if (stripMesh == null)
		{
			stripMesh = BuildMesh(DEFAULT_STRIP_RESOLUTION);
		}

		return stripMesh;
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

	}
}
