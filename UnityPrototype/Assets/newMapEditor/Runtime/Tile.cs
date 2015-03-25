using UnityEngine;
using System.Collections;

public class Tile : MonoBehaviour
{
	[SerializeField]
	private Tileset targetTileset;

	public string tileGroup = null;
	public string tileType = "basic";

	public bool isStatic = true;
	public bool includeInRandomSelection = true;

	public string largeTileGroup = null;
	public int groupX = -1;
	public int groupY = -1;
	public bool indivisibleGroup = false;

	public bool flatCollider = true;
	
	public Tileset TargetTileset
	{
		get
		{
			return targetTileset;
		}
	}

	public TileDefinition GetTileDefinition()
	{
		TileDefinition result = new TileDefinition(tileType);

		if (largeTileGroup != null && largeTileGroup.Length > 0)
		{
			result.groupName = largeTileGroup;
			Vector3 originCenter = transform.TransformPoint(new Vector3(-groupX, -0.5f, -groupY));
			result.SetGroupOrigin(originCenter);
		}

		return result;
	}
}
