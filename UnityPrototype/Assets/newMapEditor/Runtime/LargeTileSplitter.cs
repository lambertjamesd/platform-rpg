using UnityEngine;
using System.Collections;

public class LargeTileSplitter : MonoBehaviour {

	[SerializeField]
	private string tileType;

	[SerializeField]
	private int width;

	[SerializeField]
	private int height;

	[SerializeField]
	private bool indivisibleTile;

	[SerializeField]
	private UnityEngine.Object[] targetPrefabs;

	[SerializeField]
	private Mesh colliderMesh;
	[SerializeField]
	private PhysicMaterial physicMaterial;

	[SerializeField]
	private Tileset targetTileset;

	public string TileType
	{
		get
		{
			return tileType;
		}
	}

	public int Width
	{
		get
		{
			return width;
		}
	}

	public int Height
	{
		get
		{
			return height;
		}
	}

	public bool IndivisibleTile
	{
		get
		{
			return indivisibleTile;
		}
	}

	public void Resize(int newWidth, int newHeight)
	{
		UnityEngine.Object[] newObjects = new UnityEngine.Object[newWidth * newHeight];

		for (int x = 0; x < width; ++x)
		{
			for (int y = 0; y < height; ++y)
			{
				if (x < newWidth && y < newHeight)
				{
					newObjects[x + y * newWidth] = targetPrefabs[x + y * width];
				}
			}
		}

		targetPrefabs = newObjects;
		width = newWidth;
		height = newHeight;
	}

	public UnityEngine.Object GetTargetPrefab(int x, int y)
	{
		return targetPrefabs[x + y * width];
	}
	
	
	public void SetTargetPrefab(int x, int y, UnityEngine.Object value)
	{
		targetPrefabs[x + y * width] = value;
	}

	public Mesh ColliderMesh
	{
		get
		{
			return colliderMesh;
		}
	}

	public PhysicMaterial PhysicColliderMaterial
	{
		get
		{
			return physicMaterial;
		}
	}

	public Tileset TargetTileset
	{
		get
		{
			return targetTileset;
		}
	}
}
