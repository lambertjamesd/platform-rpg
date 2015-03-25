using UnityEngine;
using System.Collections;

public class LargeEdgeSplitter : MonoBehaviour {
	
	[SerializeField]
	private Tileset targetTileset;

	[SerializeField]
	private string edgeType;
	
	[SerializeField]
	private int width;

	[SerializeField]
	private bool reverseIndices;
	
	[SerializeField]
	private bool[] usedEdgeAngles = new bool[3];
	
	[SerializeField]
	private string tileTypeA;
	
	[SerializeField]
	private string tileTypeB;
	
	[SerializeField]
	private bool[] typeASides = new bool[4];
	
	[SerializeField]
	private bool[] typeBSides = new bool[4];
	
	[SerializeField]
	private UnityEngine.Object[] targetPrefabs;

	public Tileset TargetTileset
	{
		get 
		{
			return targetTileset;
		}
	}

	public string EdgeType
	{
		get
		{
			return edgeType;
		}
	}

	public int Width
	{
		get
		{
			return width;
		}
	}
	
	public bool ReverseIndices
	{
		get
		{
			return reverseIndices;
		}
	}

	public void Resize(int value)
	{
		UnityEngine.Object[] newObjects = new UnityEngine.Object[value];

		for (int i = 0; targetPrefabs != null && i < value && i < targetPrefabs.Length; ++i)
		{
			newObjects[i] = targetPrefabs[i];
		}

		targetPrefabs = newObjects;
		width = value;
	}

	public bool DoesUseEdgeAngle(EdgeAngle edgeAngle)
	{
		return usedEdgeAngles[(int)edgeAngle];
	}

	public string TileTypeA
	{
		get
		{
			return tileTypeA;
		}
	}

	public string TileTypeB
	{
		get
		{
			return tileTypeB;
		}
	}

	public bool DoesUseTypeASide(TileSide tileSide)
	{
		return typeASides[(int)tileSide];
	}

	public bool DoesUseTypeBSide(TileSide tileSide)
	{
		return typeBSides[(int)tileSide];
	}

	public UnityEngine.Object GetTargetPrefab(int index)
	{
		return targetPrefabs[index];
	}

	public void SetTargetPrefab(int index, UnityEngine.Object value)
	{
		targetPrefabs[index] = value;
	}
}
