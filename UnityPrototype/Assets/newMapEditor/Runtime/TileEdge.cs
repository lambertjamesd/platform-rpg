using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum TileSide
{
	Right,
	Bottom,
	Left,
	Top
}

public enum EdgeAngle
{
	WallAngle,
	FlatAngle,
	CliffAngle
}

[System.Serializable]
public class TileInfoType
{
	// the type of tile or null if it 
	// can match any tile
	[SerializeField]
	private string tileType;

	// the sides of the tile that are matched
	// 0 = right, 1 = bottm, 2 = left, 3 = top
	[SerializeField]
	private bool[] tileSides;

	[SerializeField]
	private bool replaceTile;

	public TileInfoType(string type)
	{
		tileType = type;
		tileSides = new bool[4];
	}

	public string TileType
	{
		get
		{
			if (tileType == null || tileType.Length == 0)
			{
				return null;
			}
			else
			{
				return tileType;
			}
		}

		set
		{
			tileType = value;
		}
	}

	public int RotationCount
	{
		get
		{
			int result = 0;

			foreach (bool value in tileSides)
			{
				if (value)
				{
					++result;
				}
			}

			return result;
		}
	}

	public bool DoesMatchSide(TileSide side)
	{
		return tileSides[(int)side];
	}

	public void SetDoesMatchSide(TileSide side, bool value)
	{
		tileSides[(int)side] = value;
	}

	public bool ReplaceTile
	{
		get
		{
			return replaceTile;
		}

		set
		{
			replaceTile = value;
		}
	}
}

public class TileEdge : MonoBehaviour
{
	[SerializeField]
	private Tileset targetTileset;

	[SerializeField]
	private string edgeType;

	[SerializeField]
	private int groupIndex = -1;
	[SerializeField]
	private int groupSize = 0;

	[SerializeField]
	private bool[] usedEdgeAngles = new bool[3];

	[SerializeField]
	private TileInfoType tileA = new TileInfoType(null);

	[SerializeField]
	private TileInfoType tileB = new TileInfoType(null);

	private int CountBools(bool[] array)
	{
		int result = 0;
		
		foreach (bool value in array)
		{
			if (value)
			{
				++result;
			}
		}
		
		return result;
	}

	public Tileset TargetTileset
	{
		get
		{
			return targetTileset;
		}
	}

	public int EdgeJointCount
	{
		get
		{
			return CountBools(usedEdgeAngles);
		}
	}

	public int ASideCount
	{
		get
		{
			return tileA.RotationCount;
		}
	}
	
	public int BSideCount
	{
		get
		{
			return tileB.RotationCount;
		}
	}

	public string EdgeType
	{
		get
		{
			return edgeType;
		}

		set
		{
			edgeType = value;
		}
	}

	public int GroupIndex
	{
		get
		{
			return groupIndex;
		}

		set
		{
			groupIndex = value;
		}
	}
	
	public int GroupSize
	{
		get
		{
			return groupSize;
		}

		set
		{
			groupSize = value;
		}
	}

	public bool UseEdgeAngle(EdgeAngle edgeAngle)
	{
		return usedEdgeAngles[(int)edgeAngle];
	}

	public void SetUseEdgeAngle(EdgeAngle edgeAngle, bool value)
	{
		usedEdgeAngles[(int)edgeAngle] = value;
	}

	public string TileTypeA
	{
		get
		{
			return tileA.TileType;
		}

		set
		{
			tileA.TileType = value;
		}
	}

	public TileInfoType TileA
	{
		get
		{
			return tileA;
		}
	}

	public TileInfoType TileB
	{
		get
		{
			return tileB;
		}
	}
	
	public string TileTypeB
	{
		get
		{
			return tileB.TileType;
		}

		set
		{
			tileB.TileType = value;
		}
	}

	public bool UseTypeASide(TileSide tileSide)
	{
		return tileA.DoesMatchSide(tileSide);
	}

	public void SetUseTypeASide(TileSide tileSide, bool value)
	{
		tileA.SetDoesMatchSide(tileSide, value);
	}

	public bool UseTypeBSide(TileSide tileSide)
	{
		return tileB.DoesMatchSide(tileSide);
	}

	public void SetUseTypeBSide(TileSide tileSide, bool value)
	{
		tileB.SetDoesMatchSide(tileSide, value);
	}

	// returns 
	public int CompatibilityScore(string typeNameA, TileSide sideA, string typeNameB, TileSide sideB, EdgeAngle edgeAngle, int edgeOffset)
	{
		bool aMatches = typeNameA == tileA.TileType;
		bool bMatches = tileB.TileType == null || tileB.TileType == typeNameB;
		bool groupMatches = true;

		if (groupIndex != -1)
		{
			edgeOffset %= groupSize;

			if (edgeOffset < 0)
			{
				edgeOffset += groupSize;
			}

			groupMatches = edgeOffset == groupIndex;
		}

		if (aMatches && bMatches && usedEdgeAngles[(int)edgeAngle] && tileA.DoesMatchSide(sideA) && tileB.DoesMatchSide(sideB) && groupMatches)
		{
			int result = 0;

			if (tileB.TileType != null)
			{
				result += 1;
			}

			result <<= 1;

			if (groupIndex != -1)
			{
				result += 1;
			}

			result <<= 1;

			if (EdgeJointCount == 1)
			{
				result += 1;
			}
			
			result <<= 1;

			if (ASideCount == 1)
			{
				result += 1;
			}

			result <<= 1;

			if (BSideCount == 1)
			{
				result += 1;
			}

			return result;
		}
		else
		{
			return -1;
		}
	}
}
