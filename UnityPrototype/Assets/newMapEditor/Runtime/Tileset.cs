using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct TileDefinition
{
	public TileDefinition(string typeName)
	{
		this.typeName = typeName;
		this.groupName = null;
		this.groupOrigin = Vector3.zero;
	}

	public TileDefinition(string typeName, string groupName)
	{
		this.typeName = typeName;
		this.groupName = groupName;
		this.groupOrigin = Vector3.zero;
	}

	public void SetGroupOrigin(Vector3 position)
	{
		groupOrigin.x = Mathf.Floor(position.x);
		groupOrigin.y = Mathf.Floor(position.y);
		groupOrigin.z = Mathf.Floor(position.z);
	}

	public string typeName;
	public string groupName;
	public Vector3 groupOrigin;

	public bool IsGroupTile
	{
		get
		{
			return groupName != null && groupName.Length > 0;
		}
	}

	public bool IsNullTile
	{
		get
		{
			return typeName == null;
		}
	}

	public void GetGroupPosition(Vector3 voxelPosition, Quaternion faceOrientation, out int groupX, out int groupY)
	{
		Vector3 flooredVoxel = new Vector3(
			Mathf.Floor(voxelPosition.x),
			Mathf.Floor(voxelPosition.y),
			Mathf.Floor(voxelPosition.z));

		Vector3 localPosition = Quaternion.Inverse(faceOrientation) * (flooredVoxel - groupOrigin);

		groupX = Mathf.FloorToInt(localPosition.x + 0.5f);
		groupY = Mathf.FloorToInt(localPosition.z + 0.5f);
	}
}

public class EdgeIndex
{
	private Dictionary<string, List<TileEdge>> edgesForTypes = new Dictionary<string, List<TileEdge>>();

	public void Cleanup()
	{
		List<string> toRemove = new List<string>();

		foreach (KeyValuePair<string, List<TileEdge>> pair in edgesForTypes)
		{
			pair.Value.RemoveAll((TileEdge e) => { return e == null; });

			if (pair.Value.Count == 0)
			{
				toRemove.Add(pair.Key);
			}
		}

		foreach (string key in toRemove)
		{
			edgesForTypes.Remove(key);
		}
	}

	private List<TileEdge> GetEdgeList(string tileType)
	{
		if (!edgesForTypes.ContainsKey(tileType))
		{
			edgesForTypes[tileType] = new List<TileEdge>();
		}

		return edgesForTypes[tileType];
	}

	public void AddEdge(TileEdge edge)
	{
		if (edge.TileTypeA != null)
		{
			GetEdgeList(edge.TileTypeA).Add(edge);
		}
	}

	public void RemoveEdge(TileEdge edge)
	{
		GetEdgeList(edge.TileTypeA).Remove(edge);
	}

	public List<TileEdge> GetPossibleEdges(string typeA, TileSide sideA, string typeB, TileSide sideB, EdgeAngle edgeAngle, int edgeOffset)
	{
		if (!edgesForTypes.ContainsKey(typeA))
		{
			return null;
		}

		List<TileEdge> result = new List<TileEdge>();
		int currentScore = 0;

		List<TileEdge> options = edgesForTypes[typeA];

		foreach (TileEdge option in options)
		{
			int score = option.CompatibilityScore(typeA, sideA, typeB, sideB, edgeAngle, edgeOffset);

			if (score > currentScore)
			{
				result.Clear();
				currentScore = score;
			}

			if (score == currentScore)
			{
				result.Add(option);
			}
		}

		return result;
	}

	public TileEdge RandomTileEdge(string typeA, TileSide sideA, string typeB, TileSide sideB, EdgeAngle edgeAngle, int edgeOffset)
	{
		List<TileEdge> options = GetPossibleEdges(typeA, sideA, typeB, sideB, edgeAngle, edgeOffset);

		if (options != null && options.Count > 0)
		{
			return options[Random.Range(0, options.Count)];
		}
		else
		{
			return null;
		}
	}
}

public class TileType {
	private int randomTileCount;
	private List<Tile> tiles = new List<Tile>();

	private List<string> tileGroups = new List<string>();
	private Dictionary<string, Vector2> tileGroupSizes = new Dictionary<string, Vector2>();

	private void UpdateTileGroupSize(string group, int x, int y)
	{
		Vector2 currentSize = Vector2.zero;

		if (tileGroupSizes.ContainsKey(group))
		{
			currentSize = tileGroupSizes[group];
		}

		currentSize.x = Mathf.Max(currentSize.x, x + 1.0f);
		currentSize.y = Mathf.Max(currentSize.y, y + 1.0f);

		tileGroupSizes[group] = currentSize;
	}

	public void AddTile(Tile tile)
	{
		if (!tiles.Contains(tile))
		{
			if (tile.includeInRandomSelection)
			{
				tiles.Insert(0, tile);
				++randomTileCount;
			}
			else
			{
				tiles.Add(tile);

				if (tile.largeTileGroup != null && tile.largeTileGroup.Length > 0)
				{
					if (!tileGroups.Contains(tile.largeTileGroup))
					{
						tileGroups.Add(tile.largeTileGroup);
						tileGroups.Sort();
					}

					UpdateTileGroupSize(tile.largeTileGroup, tile.groupX, tile.groupY);
				}
			}
		}
	}

	private void RebuildGroupIndex()
	{
		tileGroupSizes.Clear();
		
		for (int i = randomTileCount; i < tiles.Count; ++i)
		{
			Tile tile = tiles[i];

			if (tile.largeTileGroup != null && tile.largeTileGroup.Length > 0)
			{
				UpdateTileGroupSize(tile.largeTileGroup, tile.groupX, tile.groupY);
			}
		}

		tileGroups.RemoveRange(0, tileGroups.Count);
		tileGroups.AddRange(tileGroupSizes.Keys);
		tileGroups.Sort();
	}

	public void RemoveTile(Tile tile)
	{
		if (tiles.Contains(tile))
		{
			tiles.Remove(tile);

			if (tile.includeInRandomSelection)
			{
				--randomTileCount;
			}
			else
			{
				RebuildGroupIndex();
			}
		}
	}

	public void Cleanup()
	{
		tiles.RemoveAll((Tile t) => { return t == null; });

		randomTileCount = 0;
		while (randomTileCount < tiles.Count && tiles[randomTileCount].includeInRandomSelection)
		{
			++randomTileCount;
		}

		RebuildGroupIndex();
	}

	public Tile FirstTile()
	{
		return tiles[0];
	}

	public Tile RandomTile()
	{
		if (randomTileCount == 0)
		{
			return null;
		}
		else
		{
			return tiles[Random.Range(0, randomTileCount)];
		}
	}

	public int RandomTileCount
	{
		get
		{
			return randomTileCount;
		}
	}

	public bool IsEmpty
	{
		get
		{
			return tiles.Count == 0;
		}
	}

	public List<string> TileGroups
	{
		get
		{
			return tileGroups;
		}
	}

	public int TileCount
	{
		get
		{
			return tiles.Count;
		}
	}

	public Tile TileAtIndex(int index)
	{
		return tiles[index];
	}

	public void TileGroupSize(string group, out int width, out int height)
	{
		if (tileGroupSizes.ContainsKey(group))
		{
			Vector2 result = tileGroupSizes[group];

			width = (int)result.x;
			height = (int)result.y;
		}
		else
		{
			width = 0;
			height = 0;
		}
	}

	public Tile TileFromGroup(string group, int x, int y)
	{
		int groupWidth;
		int groupHeight;

		TileGroupSize(group, out groupWidth, out groupHeight);

		if (groupWidth == 0 || groupHeight == 0)
		{
			return null;
		}

		x %= groupWidth;
		y %= groupHeight;

		if (x < 0)
		{
			x += groupWidth;
		}

		if (y < 0)
		{
			y += groupHeight;
		}

		for (int i = randomTileCount; i < tiles.Count; ++i)
		{
			Tile tile = tiles[i];

			if (tile.groupX == x && tile.groupY == y && tile.largeTileGroup == group)
			{
				return tile;
			}
		}

		return null;
	}
}

public class TileIndex {
	private List<string> tileTypeNames = new List<string>();
	private Dictionary<string, TileType> tileTypes = new Dictionary<string, TileType>();

	public void AddTile(Tile tile)
	{
		if (!tileTypeNames.Contains(tile.tileType))
		{
			tileTypeNames.Add(tile.tileType);
			tileTypeNames.Sort();

			tileTypes.Add(tile.tileType, new TileType());
		}

		tileTypes[tile.tileType].AddTile(tile);
	}

	public void RemoveTile(Tile tile)
	{
		if (tileTypes.ContainsKey(tile.tileType))
		{
			tileTypes[tile.tileType].RemoveTile(tile);

			if (tileTypes[tile.tileType].IsEmpty)
			{
				tileTypes.Remove(tile.tileType);
				tileTypeNames.Remove(tile.tileType);
			}
		}
	}

	public List<string> TileTypes
	{
		get
		{
			return tileTypeNames;
		}
	}

	public void Cleanup()
	{
		List<string> toRemove = new List<string>();

		foreach (KeyValuePair<string, TileType> pair in tileTypes)
		{
			pair.Value.Cleanup();

			if (pair.Value.IsEmpty)
			{
				toRemove.Add(pair.Key);
			}
		}

		foreach (string tileType in toRemove)
		{
			tileTypes.Remove(tileType);
			tileTypeNames.Remove(tileType);
		}
	}

	public Tile FirstTileOfType(string type)
	{
		if (tileTypes.ContainsKey(type))
		{
			return tileTypes[type].FirstTile();
		}
		else 
		{
			return null;
		}
	}

	public Tile RandomTile(string type)
	{
		if (tileTypes.ContainsKey(type))
		{
			return tileTypes[type].RandomTile();
		}
		else 
		{
			return null;
		}
	}

	public Tile TileFromGroup(string type, string group, int x, int y)
	{
		if (tileTypes.ContainsKey(type))
		{
			return tileTypes[type].TileFromGroup(group, x, y);
		}
		else 
		{
			return null;
		}
	}

	public int NumberOfTiles(string type)
	{
		if (tileTypes.ContainsKey(type))
		{
			return tileTypes[type].TileCount;
		}
		else 
		{
			return 0;
		}
	}

	public int NumberOfRandomTiles(string type)
	{
		if (tileTypes.ContainsKey(type))
		{
			return tileTypes[type].RandomTileCount;
		}
		else 
		{
			return 0;
		}
	}

	public Tile TileAtIndex(string type, int index)
	{
		if (tileTypes.ContainsKey(type))
		{
			return tileTypes[type].TileAtIndex(index);
		}
		else 
		{
			return null;
		}
	}

	public List<string> GroupsForType(string type)
	{
		if (tileTypes.ContainsKey(type))
		{
			return tileTypes[type].TileGroups;
		}
		else 
		{
			return null;
		}
	}
}


public class Tileset : ScriptableObject {
	public string tilesetName = "Tileset";
	[SerializeField]
	private List<Tile> tiles = new List<Tile>();

	[SerializeField]
	private List<PlaceableObject> placeableObjects = new List<PlaceableObject>();

	private TileIndex tileIndex = null;

	[SerializeField]
	private List<TileEdge> edges = new List<TileEdge>();
	private EdgeIndex edgeIndex = null;

	[SerializeField]
	private List<TileCorner> corners = new List<TileCorner>();
	private TileCornerIndex cornerIndex = null;

	public void Cleanup()
	{
		CheckInit();

		tiles.RemoveAll((Tile t) => { return t == null; });
		placeableObjects.RemoveAll((PlaceableObject o) => { return o == null; });
		edges.RemoveAll((TileEdge e) => { return e == null; });

		tileIndex.Cleanup();
		edgeIndex.Cleanup();
	}

	private void Init()
	{
		tileIndex = new TileIndex();

		foreach (Tile tile in tiles)
		{
			tileIndex.AddTile(tile);
		}

		edgeIndex =  new EdgeIndex();

		foreach (TileEdge edge in edges)
		{
			edgeIndex.AddEdge(edge);
		}

		cornerIndex = new TileCornerIndex();

		foreach (TileCorner corner in corners)
		{
			cornerIndex.AddCorner(corner);
		}
	}

	private void CheckInit()
	{
		if (tileIndex == null)
		{
			Init();
		}
	}

	public void RebuildIndex()
	{
		Init();
	}

	public int TileTypeCount
	{
		get
		{
			CheckInit();
			return tileIndex.TileTypes.Count;
		}
	}

	public string GetTileType(int index)
	{
		CheckInit();
		return tileIndex.TileTypes[index];
	}

	public IEnumerable<string> TileTypes
	{
		get
		{
			CheckInit();
			return tileIndex.TileTypes;
		}
	}

	public void AddTile(Tile tile)
	{
		if (!tiles.Contains(tile))
		{
			CheckInit();
			tiles.Add(tile);
			tileIndex.AddTile(tile);
		}
	}

	public void RemoveTile(Tile tile)
	{
		if (tiles.Contains(tile))
		{
			CheckInit();
			tiles.Remove(tile);
			tileIndex.RemoveTile(tile);
		}
	}

	public Tile AnyTile()
	{
		foreach (Tile tile in tiles)
		{
			if (tile.largeTileGroup == null || tile.largeTileGroup.Length == 0)
			{
				return tile;
			}
		}

		return null;
	}

	public Tile FirstTileOfType(string type)
	{
		CheckInit();
		return tileIndex.FirstTileOfType(type);
	}

	public Tile RandomTileOfType(string type)
	{
		CheckInit();
		return tileIndex.RandomTile(type);
	}

	public Tile FindTile(TileDefinition tileDef, Vector3 voxelCenter, Quaternion faceOrientation)
	{
		if (tileDef.IsGroupTile)
		{
			int groupX;
			int groupY;

			tileDef.GetGroupPosition(voxelCenter, faceOrientation, out groupX, out groupY);

			return TileFromGroup(tileDef.typeName, tileDef.groupName, groupX, groupY);
		}
		else
		{
			return RandomTileOfType(tileDef.typeName);
		}
	}
	
	public Tile TileFromGroup(string type, string group, int x, int y)
	{
		CheckInit();
		return tileIndex.TileFromGroup(type, group, x, y);
	}

	public int GetNumberOfType(string type)
	{
		CheckInit();
		return tileIndex.NumberOfTiles(type);
	}
	
	public int NumberOfRandomTiles(string type)
	{
		CheckInit();
		return tileIndex.NumberOfRandomTiles(type);
	}

	public List<string> GroupsForType(string type)
	{
		CheckInit();
		return tileIndex.GroupsForType(type);
	}
	
	public Tile GetTile(string type, int index)
	{
		CheckInit();
		return tileIndex.TileAtIndex(type, index);
	}

	public void AddPlaceableObject(PlaceableObject value)
	{
		if (!placeableObjects.Contains(value))
		{
			placeableObjects.Add(value);
		}
	}

	public void RemovePlacableObject(PlaceableObject value)
	{
		placeableObjects.Remove(value);
	}

	public int PlacableObjectCount
	{
		get
		{
			return placeableObjects.Count;
		}
	}

	public PlaceableObject GetPlaceableObject(int index)
	{
		return placeableObjects[index];
	}

	public int TileEdgeCount
	{
		get
		{
			return edges.Count;
		}
	}

	public TileEdge GetEdge(int index)
	{
		return edges[index];
	}

	public void AddTileEdge(TileEdge edge)
	{
		if (!edges.Contains(edge))
		{
			CheckInit();

			edgeIndex.AddEdge(edge);
			edges.Add(edge);
		}
	}

	public void RemoveTileEdge(TileEdge edge)
	{
		CheckInit();
		edgeIndex.RemoveEdge(edge);
		edges.Remove(edge);
	}

	public TileEdge RandomTileEdge(string typeA, TileSide sideA, string typeB, TileSide sideB, EdgeAngle edgeAngle, int edgeOffset)
	{
		return edgeIndex.RandomTileEdge(typeA, sideA, typeB, sideB, edgeAngle, edgeOffset);
	}

	public int TileCornerCount
	{
		get
		{
			return corners.Count;
		}
	}

	public TileCorner GetCorner(int index)
	{
		return corners[index];
	}

	public void AddCorner(TileCorner corner)
	{
		if (!corners.Contains(corner))
		{
			CheckInit();
			cornerIndex.AddCorner(corner);
			corners.Add(corner);
		}
	}

	public void RemoveCorner(TileCorner corner)
	{
		CheckInit();
		cornerIndex.RemoveCorner(corner);
		corners.Remove(corner);
	}

	public TileCorner FindCorner(EdgeAngle[] edgeAngles, string[] tileTypes, TileSide[] tileRotations, int offset)
	{
		CheckInit();
		return cornerIndex.FindCorner(edgeAngles, tileTypes, tileRotations, offset);
	}
}

