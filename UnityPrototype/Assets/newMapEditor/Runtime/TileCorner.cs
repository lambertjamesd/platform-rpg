using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TileCorner : MonoBehaviour {

	// To represent a corner types I use a series of edge
	// angles. A wall edge angle forms a concave corner
	// like where the floor meets the wall. A flat edge
	// are just where two tiles facing the same way
	// meet and a cliff angle is a convex corner like
	// when the floor bends downward into a cliff
	// To repesent the corner, I first step to an
	// adjacent tile then turn 90 degrees to the left
	// and repeat this intil I arrive at the starting
	// tile. The edge angle types that are crossed
	// while stepping over the tiles form the array
	// of EdgeAngles that represent the shape of a corner.
	// As a simple example, three flat edges form a corner
	// intersection where there are four tiles that
	// meet on a flat plane
	//
	//   ---------------
	//    \    | \ <-   \
	//     \   v  \      \
	//      --------------
	//      \      \  ^   \
	//       \   -> \ |    \
	//        -------------- 
	//
	// Two cliff edge angles make a convex corner
	//
	//    --------
	//    |\      \
	//    | \ <-   \
	//    |  -------
	//    \->|  ^   |
	//     \ |  |   |
	//      \|      |
	//       -------
	//
	// Two wall edge angles make a concave corner
	// 
	//   --------
	//   |      |\
	//   |   |  | \
	//   |   v  |<-|
	//   --------  |
	//    \    ->\ |
	//     \      \|
	//      --------
	//
	// There are many different corner shape types and
	// edge angles can represent all of them
	//
	// 2 edge angles
	// c c = convex corner
	// w w = concave corner
	//
	// 3 edge angles
	// f f f = flat
	// 

	[SerializeField]
	private Tileset targetTileset;

	[SerializeField]
	private EdgeAngle[] edgeAngles;

	private int cornerType = -1;

	// the types of tiles the corner is connected to
	// the length of this array will equal the length
	// of edgeAngles
	[SerializeField]
	private TileInfoType[] tileTypes;

	public Tileset TargetTileset
	{
		get
		{
			return targetTileset;
		}

		set
		{
			targetTileset = value;
		}
	}

	public int CornerType
	{
		get
		{
			if (cornerType == -1)
			{
				cornerType = CalculateCornerTypeSignature(edgeAngles, 0);
			}

			return cornerType;
		}
	}

	public static int CalculateCornerTypeSignature(EdgeAngle[] edgeAngles, int startOffset)
	{
		// start the value at one to encode array length
		int result = 1;

		for (int i = 0; i < edgeAngles.Length; ++i)
		{
			result *= 3;
			result += (int)edgeAngles[(i + startOffset) % edgeAngles.Length];
		}

		return result;
	}

	public int MatchScore(string[] tileTypeNames, TileSide[] tileRotations, int sourceOffset)
	{
		int exactTypeMatchCount = 0;
		int rotationScore = 0;

		for (int i = 0; i < tileTypes.Length; ++i)
		{
			int sourceIndex = (i + sourceOffset) % tileTypes.Length;

			if (tileTypeNames[sourceIndex] != tileTypes[i].TileType && tileTypes[i].TileType != null)
			{
				// doesn't match type
				return -1;
			}

			if (tileTypeNames[sourceIndex] == tileTypes[i].TileType)
			{
				++exactTypeMatchCount;
			}

			if (!tileTypes[i].DoesMatchSide(tileRotations[sourceIndex]))
			{
				// doesn't match rotation
				return -1;
			}

			rotationScore += 4 - tileTypes[i].RotationCount;
		}

		// give matching more tile types a higher priority than matching rotation
		return exactTypeMatchCount * tileTypes.Length * 3 + rotationScore;
	}

	public int EdgeAngleCount
	{
		get
		{
			return edgeAngles == null ? 0 : edgeAngles.Length - 1;
		}

		set
		{
			EdgeAngle[] newEdgeAngles = new EdgeAngle[value + 1];
			TileInfoType[] newTileTypes = new TileInfoType[value + 1];

			for (int i = 0; i <= value; ++i)
			{
				if (edgeAngles != null && i < edgeAngles.Length && i != value)
				{
					newEdgeAngles[i] = edgeAngles[i];
				}

				if (tileTypes != null && i < tileTypes.Length)
				{
					newTileTypes[i] = tileTypes[i];
				}
				else
				{
					newTileTypes[i] = new TileInfoType(null);
				}
			}

			edgeAngles = newEdgeAngles;
			tileTypes = newTileTypes;

			RecalcFinalEdge();
		}
	}

	public EdgeAngle GetEdgeAngle(int index)
	{
		return edgeAngles[index];
	}

	public void SetEdgeAngle(int index, EdgeAngle value)
	{
		edgeAngles[index] = value;
		RecalcFinalEdge();
	}

	public TileInfoType GetTileType(int index)
	{
		return tileTypes[index];
	}

	public void SetTileType(int index, TileInfoType value)
	{
		tileTypes[index] = value;
	}

	// the final edge angle is redundant
	// but is necesarry to calculate the
	// corner signature. This reclacultes
	// the final edge type based on
	// all other edge types
	public void RecalcFinalEdge()
	{
		cornerType = -1;

		Quaternion currentRotation = Quaternion.identity;

		for (int i = 0; i < edgeAngles.Length - 1; ++i)
		{
			currentRotation = currentRotation * StepRotation(edgeAngles[i]);
		}

		Vector3 finalFaceDirection = currentRotation * Vector3.up;

		EdgeAngle result;

		if (finalFaceDirection.z > 0.5f)
		{
			result = EdgeAngle.CliffAngle;
		}
		else if (finalFaceDirection.z < -0.5f)
		{
			result = EdgeAngle.WallAngle;
		}
		else
		{
			result = EdgeAngle.FlatAngle;
		}

		edgeAngles[edgeAngles.Length - 1] = result;
	}

	public bool IsValidCornerSignature
	{
		get
		{
			Quaternion currentRotation = Quaternion.identity;
			
			for (int i = 0; i < edgeAngles.Length; ++i)
			{
				currentRotation = currentRotation * StepRotation(edgeAngles[i]);
			}

			if (currentRotation.w < 0.0f)
			{
				currentRotation.w *= -1.0f;
				currentRotation.x *= -1.0f;
				currentRotation.y *= -1.0f;
				currentRotation.z *= -1.0f;
			}

			// after traversing edges, the face orienation should
			// match the identity
			float angle;
			Vector3 axis;
			currentRotation.ToAngleAxis(out angle, out axis);

			// take rounding errors into account
			return Mathf.Abs(angle) < 5.0f;
		}
	}

	public static Quaternion StepRotation(EdgeAngle edgeAngle)
	{
		return Quaternion.AngleAxis(-90.0f * ((int) edgeAngle - 1), Vector3.forward) * Quaternion.AngleAxis(-90.0f, Vector3.up);
	}
}

public class TileCornerIndex
{
	// maps corner type signatures to a list of possible corner fits
	private Dictionary<int, List<TileCorner>> index = new Dictionary<int, List<TileCorner>>(); 

	public List<TileCorner> PotentailCorners(EdgeAngle[] edgeAngles, string[] tileTypes, TileSide[] tileRotations, int offset)
	{
		int signature = TileCorner.CalculateCornerTypeSignature(edgeAngles, offset);

		if (index.ContainsKey(signature))
		{
			List<TileCorner> potentialResults = index[signature];
			List<TileCorner> result = null;
			int currentScore = 0;

			foreach (TileCorner corner in potentialResults)
			{
				int cornerScore = corner.MatchScore(tileTypes, tileRotations, offset);

				if (cornerScore > currentScore)
				{
					if (result == null)
					{
						result = new List<TileCorner>();
					}
					else
					{
						result.Clear();
					}

					currentScore = cornerScore;
				}

				if (currentScore == cornerScore)
				{
					result.Add(corner);
				}
			}

			return result;
		}

		return null;
	}
	
	public TileCorner FindCorner(EdgeAngle[] edgeAngles, string[] tileTypes, TileSide[] tileRotations, int offset)
	{
		List<TileCorner> options = PotentailCorners(edgeAngles, tileTypes, tileRotations, offset);

		if (options == null)
		{
			return null;
		}
		else
		{
			return options[Random.Range(0, options.Count)];
		}
	}

	public void Cleanup()
	{
		List<int> toRemove = new List<int>();
		
		foreach (KeyValuePair<int, List<TileCorner>> pair in index)
		{
			pair.Value.RemoveAll((TileCorner e) => { return e == null; });
			
			if (pair.Value.Count == 0)
			{
				toRemove.Add(pair.Key);
			}
		}
		
		foreach (int key in toRemove)
		{
			index.Remove(key);
		}
	}

	private List<TileCorner> GetCornerList(int cornerType)
	{
		if (!index.ContainsKey(cornerType))
		{
			index[cornerType] = new List<TileCorner>();
		}
		
		return index[cornerType];
	}

	public void AddCorner(TileCorner corner)
	{
		GetCornerList(corner.CornerType).Add(corner);
	}

	public void RemoveCorner(TileCorner corner)
	{
		GetCornerList(corner.CornerType).Remove(corner);
	}
}
