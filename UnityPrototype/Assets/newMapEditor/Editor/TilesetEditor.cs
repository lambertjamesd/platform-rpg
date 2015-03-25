using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(Tileset))]
public class TilesetEditor : Editor
{
	private Dictionary<string, bool> isExpanded = new Dictionary<string, bool>();

	private bool areObjectsExpanded = false;
	private bool areEdgeJointsExpanded = false;
	private bool areCornerJointsExpanded = false;

	private delegate void DeleteCallback();

	private static void ObjectPreview(GameObject gameObject, DeleteCallback deleteCallback)
	{
		Texture2D objectPreview = AssetPreview.GetAssetPreview(gameObject);
		
		EditorGUILayout.BeginHorizontal(GUILayout.Height(80.0f));
		EditorGUILayout.LabelField(new GUIContent(gameObject.name, objectPreview), GUILayout.Height(80.0f));
		
		if (GUILayout.Button("delete"))
		{
			deleteCallback();
		}

		if (GUILayout.Button("locate"))
		{
			EditorGUIUtility.PingObject(gameObject);
		}
		
		EditorGUILayout.EndHorizontal();
	}
	
	public void OnEnable()
	{
		((Tileset)target).Cleanup();
	}

	public override void OnInspectorGUI ()
	{
		serializedObject.Update();

		SerializedProperty tilesetName = serializedObject.FindProperty("tilesetName");
		tilesetName.stringValue = EditorGUILayout.TextField(new GUIContent("Tileset Name"), tilesetName.stringValue);
		
		EditorGUILayout.Space();

		EditorGUILayout.LabelField(new GUIContent("Drag tile here to add it to the tileset"));
		Tile newTile = (Tile)EditorGUILayout.ObjectField(null, typeof(Tile), false, GUILayout.Height(50.0f));

		Tileset tileset = (Tileset)target;

		if (newTile != null)
		{
			tileset.AddTile(newTile);
			EditorUtility.SetDirty(tileset);
		}
		
		EditorGUILayout.Space();

		if (GUILayout.Button(new GUIContent("Reload Tiles")))
		{
			tileset.RebuildIndex();
			EditorUtility.SetDirty(tileset);
		}

		EditorGUILayout.Space();

		EditorGUILayout.LabelField(new GUIContent("Tile types"));

		List<string> tileTypes = new List<string>(tileset.TileTypes);

		foreach (string tileType in tileTypes)
		{
			Tile tile = tileset.FirstTileOfType(tileType);
			Texture2D previewTexture = AssetPreview.GetAssetPreview(tile.gameObject);

			bool currentlyExpanded = isExpanded.ContainsKey(tileType) && isExpanded[tileType];
			bool nowExpanded = EditorGUILayout.Foldout(currentlyExpanded, new GUIContent(tileType, previewTexture));

			if (nowExpanded)
			{
				for (int i = 0; i < tileset.GetNumberOfType(tileType); ++i)
				{
					Tile currentTile = tileset.GetTile(tileType, i);
					ObjectPreview(currentTile.gameObject, () => {
						tileset.RemoveTile(currentTile);
						EditorUtility.SetDirty(tileset);
					});
				}
			}

			isExpanded[tileType] = nowExpanded;
		}

		EditorGUILayout.Space();

		EditorGUILayout.LabelField(new GUIContent("Drag prefab here to add it to placeable objects"));
		PlaceableObject newObject = (PlaceableObject)EditorGUILayout.ObjectField(null, typeof(PlaceableObject), false, GUILayout.Height(50.0f));

		if (newObject != null)
		{
			tileset.AddPlaceableObject(newObject);
			EditorUtility.SetDirty(tileset);
		}

		areObjectsExpanded = EditorGUILayout.Foldout(areObjectsExpanded, new GUIContent("Placeable Objects"));

		if (areObjectsExpanded)
		{
			for (int i = 0; i < tileset.PlacableObjectCount; ++i)
			{
				PlaceableObject placeableObject = tileset.GetPlaceableObject(i);

				ObjectPreview(placeableObject.gameObject, () => {
					tileset.RemovePlacableObject(placeableObject);
					EditorUtility.SetDirty(tileset);
				});
			}
		}
		
		EditorGUILayout.Space();
		
		EditorGUILayout.LabelField(new GUIContent("Drag prefab here to add it to edge joints objects"));
		TileEdge newEdge = (TileEdge)EditorGUILayout.ObjectField(null, typeof(TileEdge), false, GUILayout.Height(50.0f));
		
		if (newEdge != null)
		{
			tileset.AddTileEdge(newEdge);
			EditorUtility.SetDirty(tileset);
		}
		
		areEdgeJointsExpanded = EditorGUILayout.Foldout(areEdgeJointsExpanded, new GUIContent("Edge Joints"));

		if (areEdgeJointsExpanded)
		{
			for (int i = 0; i < tileset.TileEdgeCount; ++i)
			{
				TileEdge edgeObject = tileset.GetEdge(i);
				
				ObjectPreview(edgeObject.gameObject, () => {
					tileset.RemoveTileEdge(edgeObject);
					EditorUtility.SetDirty(tileset);
				});
			}
		}

		EditorGUILayout.Space();
		
		EditorGUILayout.LabelField(new GUIContent("Drag prefab here to add it to corner joints objects"));
		TileCorner newCorner = (TileCorner)EditorGUILayout.ObjectField(null, typeof(TileCorner), false, GUILayout.Height(50.0f));
		
		if (newCorner != null)
		{
			tileset.AddCorner(newCorner);
			EditorUtility.SetDirty(tileset);
		}
		
		areCornerJointsExpanded = EditorGUILayout.Foldout(areCornerJointsExpanded, new GUIContent("Corner Joints"));
		
		if (areCornerJointsExpanded)
		{
			for (int i = 0; i < tileset.TileCornerCount; ++i)
			{
				TileCorner cornerObject = tileset.GetCorner(i);
				
				ObjectPreview(cornerObject.gameObject, () => {
					tileset.RemoveCorner(cornerObject);
					EditorUtility.SetDirty(tileset);
				});
			}
		}

		serializedObject.ApplyModifiedProperties();
	}
}
