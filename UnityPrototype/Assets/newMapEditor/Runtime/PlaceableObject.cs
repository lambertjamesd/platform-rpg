using UnityEngine;
using System.Collections;

public class PlaceableObject : MonoBehaviour {
	
	[SerializeField]
	private Tileset targetTileset;

	[SerializeField]
	private string categoryName;
	
	[SerializeField]
	private int width = 1;
	[SerializeField]
	private int height = 1;
	[SerializeField]
	private int depth = 1;
	
	[SerializeField]
	private bool placeOnFloor = true;
	
	[SerializeField]
	private bool placeOnWall = false;

	[SerializeField]
	private bool isStatic = false;

	[SerializeField]
	private bool isArchitecture = false;

	public string CategoryName
	{
		get
		{
			return categoryName;
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

	public int Depth
	{
		get
		{
			return depth;
		}
	}

	public Vector3 Size
	{
		get
		{
			return new Vector3(width, height, depth);
		}
	}

	public bool PlaceOnFloor
	{
		get
		{
			return placeOnFloor;
		}
	}

	public bool PlaceOnWall
	{
		get
		{
			return placeOnWall;
		}
	}

	public bool IsStatic
	{
		get
		{
			return isStatic;
		}
	}

	public Vector3 RotatedSize
	{
		get
		{
			Vector3 result = transform.localRotation * Size;
			return new Vector3(
				Mathf.Floor(Mathf.Abs(result.x) + 0.5f),
				Mathf.Floor(Mathf.Abs(result.y) + 0.5f),
				Mathf.Floor(Mathf.Abs(result.z) + 0.5f));
		}
	}

	public Vector3 MinCorner
	{
		get
		{
			Vector3 halfSize = RotatedSize * 0.5f;
			Vector3 position = transform.localPosition;

			return new Vector3(Mathf.Floor(position.x - halfSize.x + 0.5f), position.y, Mathf.Floor(position.z - halfSize.z * 0.5f));
		}
	}

	public bool IsArchitecture
	{
		get
		{
			return isArchitecture;
		}
	}
}
