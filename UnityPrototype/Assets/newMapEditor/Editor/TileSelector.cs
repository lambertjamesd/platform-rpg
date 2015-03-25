using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class TileSelector : EditorWindow {

	private static readonly float targetTileSize = 128.0f;

	private Vector2 scrollPosition;

	private Tileset selectedTileset = null;
	private int selectedTileIndex = 0;
	private int selectedGroupIndex = 0;

	private int selectedCategoryIndex = 0;
	private int selectedObjectIndex = 0;
	
	private List<string> tileGroupNames = new List<string>();
	private List<string> objectCategoryNames = new List<string>();

	private static readonly string NoCategory = "No category";

	private void RebuildCategoryNames()
	{
		HashSet<string> categoryNames = new HashSet<string>();
		
		for (int index = 0; index < selectedTileset.PlacableObjectCount; ++index)
		{
			PlaceableObject placeableObject = selectedTileset.GetPlaceableObject(index);

			if (placeableObject != null)
			{
				if (placeableObject.CategoryName == null || placeableObject.CategoryName.Length == 0)
				{
					categoryNames.Add(NoCategory);
				}
				else
				{
					categoryNames.Add(placeableObject.CategoryName);
				}
			}
		}

		objectCategoryNames.Clear();
		objectCategoryNames.AddRange(categoryNames);

		objectCategoryNames.Sort();


		HashSet<string> tileGroupNameSet = new HashSet<string>();

		for (int index = 0; index < selectedTileset.TileTypeCount; ++index)
		{
			Tile tile = selectedTileset.GetTile(selectedTileset.GetTileType(index), 0);

			if (tile != null)
			{
				if (tile.tileGroup == null || tile.tileGroup.Length == 0)
				{
					tileGroupNameSet.Add(NoCategory);
				}
				else
				{
					tileGroupNameSet.Add(tile.tileGroup);
				}
			}
		}

		tileGroupNames.Clear();
		tileGroupNames.AddRange(tileGroupNameSet);
		tileGroupNames.Sort();
	}

	public enum Mode
	{
		Tiles,
		PlaceableObjects
	}

	public Mode SelectMode { get; set; }

	[MenuItem("Assets/Create/Tileset")]
	public static void CreateLevelData() {
		Tileset newTileset = ScriptableObject.CreateInstance<Tileset>();
		string filename = AssetDatabase.GetAssetPath(Selection.activeObject);
		
		if (filename != "" && Path.GetExtension(filename) != "")
		{
			filename = Path.GetDirectoryName(filename);
		}
		
		filename = AssetDatabase.GenerateUniqueAssetPath(filename + "/New Tileset.asset");
		
		AssetDatabase.CreateAsset(newTileset, filename);
		AssetDatabase.SaveAssets();
		EditorUtility.FocusProjectWindow();
		Selection.activeObject = newTileset;
	}

	[MenuItem ("Window/Map Editor/Tiles")]
	static void Init () {
		EditorWindow.GetWindow (typeof (TileSelector));
	}

	private static bool CategoryNamesMatch(string a, string b)
	{
		if (a == null || a.Length == 0)
		{
			a = NoCategory;
		}

		if (b == null || b.Length == 0)
		{
			b = NoCategory;
		}

		return a == b;
	}

	private int CategoryList(GUIContent label, List<string> names, int currentIndex, float width)
	{
		GUIContent[] categories = new GUIContent[names.Count];
		
		for (int index = 0; index < names.Count; ++index)
		{
			categories[index] = new GUIContent(names[index]);
		}
		
		return EditorGUILayout.Popup(label, currentIndex, categories, GUILayout.Width(width));
	}
	
	void OnGUI () {
		if (selectedTileset != null)
		{
			int tilesPerRow = Mathf.Max(1, Mathf.FloorToInt(position.width / targetTileSize + 0.5f));

			switch (SelectMode)
			{
			case Mode.Tiles:
			{
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
				EditorGUILayout.BeginVertical(GUILayout.Width(position.width));
				
				selectedCategoryIndex = CategoryList(new GUIContent("Category"), tileGroupNames, selectedCategoryIndex, position.width);
				string selectedGroup = tileGroupNames[selectedCategoryIndex];

				List<GUIContent> tiles = new List<GUIContent>(selectedTileset.TileTypeCount);

				int index = 0;
				int selectedTile = 0;
				foreach (string tileType in selectedTileset.TileTypes)
				{
					Tile tile = selectedTileset.FirstTileOfType(tileType);

					if (tile != null && CategoryNamesMatch(tile.tileGroup, selectedGroup))
					{
						Texture2D previewTexture = AssetPreview.GetAssetPreview(tile.gameObject);
						tiles.Add(new GUIContent(tileType, previewTexture));

						if (index < selectedTileIndex)
						{
							++selectedTile;
						}
					}

					++index;
				}

				selectedTile = GUILayout.SelectionGrid(selectedTile, tiles.ToArray(), tilesPerRow, GUILayout.Width(position.width));

				index = 0;
				foreach (string tileType in selectedTileset.TileTypes)
				{
					Tile tile = selectedTileset.FirstTileOfType(tileType);
					
					if (tile != null && CategoryNamesMatch(tile.tileGroup, selectedGroup))
					{
						if (selectedTile == 0)
						{
							selectedTileIndex = index;
							break;
						}
						else
						{
							--selectedTile;
						}
					}

					++index;
				}

				string selectedType = SelectedTileType;

				if (selectedType != null && selectedTileset.GroupsForType(selectedType) != null)
				{
					List<string> groups = selectedTileset.GroupsForType(selectedType);

					bool hasRandom = selectedTileset.NumberOfRandomTiles(selectedType) > 0;

					GUIContent[] groupGUI = new GUIContent[groups.Count + (hasRandom ? 1 : 0)];

					for (int i = 0; i < groups.Count; ++i)
					{
						Tile tile = selectedTileset.TileFromGroup(selectedType, groups[i], 0, 0);
						Texture2D previewTexture = AssetPreview.GetAssetPreview(tile.gameObject);
						groupGUI[i] = new GUIContent(groups[i], previewTexture);
					}

					if (hasRandom)
					{
						groupGUI[groups.Count] = new GUIContent("Random Tile");
					}

					selectedGroupIndex = GUILayout.SelectionGrid(selectedGroupIndex, groupGUI, tilesPerRow, GUILayout.Width(position.width));
				}
				
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndScrollView();

				break;
			}
			case Mode.PlaceableObjects:
			{
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
				EditorGUILayout.BeginVertical(GUILayout.Width(position.width));

				if (objectCategoryNames.Count == 0 || GUILayout.Button(new GUIContent("Refresh"), GUILayout.Width(position.width)))
				{
					RebuildCategoryNames();
				}

				int newCategoryIndex = CategoryList(new GUIContent("Category"), objectCategoryNames, selectedCategoryIndex, position.width);

				if (newCategoryIndex != selectedCategoryIndex)
				{
					selectedCategoryIndex = newCategoryIndex;
					selectedObjectIndex = 0;
				}

				string selectedCategory = (selectedCategoryIndex >= 0 && selectedCategoryIndex < objectCategoryNames.Count) ? objectCategoryNames[selectedCategoryIndex] : NoCategory;
				
				List<GUIContent> objects = new List<GUIContent>();

				for (int index = 0; index < selectedTileset.PlacableObjectCount; ++index)
				{
					PlaceableObject placeableObject = selectedTileset.GetPlaceableObject(index);

					if (placeableObject != null && CategoryNamesMatch(placeableObject.CategoryName, selectedCategory))
					{
						Texture2D previewTexture = AssetPreview.GetAssetPreview(placeableObject.gameObject);
						objects.Add(new GUIContent(placeableObject.name, previewTexture));
					}
				}
				
				selectedObjectIndex = GUILayout.SelectionGrid(selectedObjectIndex, objects.ToArray(), tilesPerRow, GUILayout.Width(position.width));

				EditorGUILayout.EndVertical();
				EditorGUILayout.EndScrollView();
				break;
			}
			}
		}
	}

	public Tileset SelectedTileset
	{
		get
		{
			return selectedTileset;
		}

		set
		{
			Repaint();
			selectedTileset = value;

			if (selectedTileset != null)
			{
				selectedTileset.Cleanup();
				RebuildCategoryNames();
			}

			if (selectedTileset == null || selectedTileIndex >= selectedTileset.TileTypeCount)
			{
				selectedTileIndex = 0;
			}

			if (selectedTileset == null || selectedObjectIndex >= selectedTileset.PlacableObjectCount)
			{
				selectedObjectIndex = 0;
			}
		}
	}

	public string SelectedTileType
	{
		get
		{
			if (selectedTileset != null && selectedTileIndex >= 0 && selectedTileIndex < selectedTileset.TileTypeCount)
			{
				return selectedTileset.GetTileType(selectedTileIndex);
			}
			else
			{
				return null;
			}
		}
	}

	public TileDefinition SelectedTileDefinition
	{
		get
		{
			if (selectedTileset != null && selectedTileIndex >= 0 && selectedTileIndex < selectedTileset.TileTypeCount)
			{
				string selectedType = selectedTileset.GetTileType(selectedTileIndex);
				bool hasRandom = selectedTileset.NumberOfRandomTiles(selectedType) > 0;
				
				List<string> groups = selectedTileset.GroupsForType(selectedType);

				if (selectedGroupIndex < 0 || selectedGroupIndex >= groups.Count)
				{
					if (hasRandom)
					{
						return new TileDefinition(selectedType);
					}
					else
					{
						return new TileDefinition(null);
					}
				}
				else
				{
					return new TileDefinition(selectedType, groups[selectedGroupIndex]);
				}
			}
			else
			{
				return new TileDefinition(null);
			}

		}
	}

	private int RemapSelectedObjectIndex(int selectedIndex)
	{
		string selectedCategory = (selectedCategoryIndex >= 0 && selectedCategoryIndex < objectCategoryNames.Count) ? objectCategoryNames[selectedCategoryIndex] : NoCategory;

		for (int index = 0; index < selectedTileset.PlacableObjectCount; ++index)
		{
			PlaceableObject placeableObject = selectedTileset.GetPlaceableObject(index);
			
			if (placeableObject != null && CategoryNamesMatch(placeableObject.CategoryName, selectedCategory))
			{
				if (selectedIndex == 0)
				{
					return index;
				}
				else
				{
					--selectedIndex;
				}
			}
		}

		return -1;
	}

	public PlaceableObject SelectedPlacableObject
	{
		get
		{
			if (selectedTileset != null)
			{
				int actualIndex = RemapSelectedObjectIndex(selectedObjectIndex);

				if (actualIndex != -1)
				{
					return selectedTileset.GetPlaceableObject(actualIndex);
				}
			}
			
			return null;
		}
	}
}
