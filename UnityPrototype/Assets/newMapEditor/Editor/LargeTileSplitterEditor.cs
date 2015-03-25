using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

[CustomEditor(typeof(LargeTileSplitter))]
public class LargeTileSplitterEditor : Editor {

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		SerializedProperty tileType = serializedObject.FindProperty("tileType");
		tileType.stringValue = EditorGUILayout.TextField("Tile Type", tileType.stringValue);
		
		SerializedProperty colliderMeshProperty = serializedObject.FindProperty("colliderMesh");
		colliderMeshProperty.objectReferenceValue = EditorGUILayout.ObjectField("Collider Mesh", colliderMeshProperty.objectReferenceValue, typeof(Mesh), false);
		
		SerializedProperty physicMaterialProperty = serializedObject.FindProperty("physicMaterial");
		physicMaterialProperty.objectReferenceValue = EditorGUILayout.ObjectField("Physic Material", physicMaterialProperty.objectReferenceValue, typeof(PhysicMaterial), false);
		
		SerializedProperty tilesetProperty = serializedObject.FindProperty("targetTileset");
		tilesetProperty.objectReferenceValue = EditorGUILayout.ObjectField("Target Tileset", tilesetProperty.objectReferenceValue, typeof(Tileset), false);
		
		SerializedProperty width = serializedObject.FindProperty("width");
		SerializedProperty height = serializedObject.FindProperty("height");

		int newWidth = width.intValue;
		int newHeight = height.intValue;

		if (newWidth == 0 || newHeight == 0)
		{
			LargeTileSplitter splitter = (LargeTileSplitter)target;
			MeshFilter meshFilter = splitter.GetComponent<MeshFilter>();

			if (meshFilter != null && meshFilter.sharedMesh != null)
			{
				Bounds meshBounds = meshFilter.sharedMesh.bounds;

				newWidth = Mathf.FloorToInt(meshBounds.size.x + 0.5f);
				newHeight = Mathf.FloorToInt(meshBounds.size.z + 0.5f);
			}
		}

		newWidth = Mathf.Max(1, EditorGUILayout.IntField("Width", newWidth));
		newHeight = Mathf.Max(1, EditorGUILayout.IntField("Height", newHeight));

		if (width.intValue != newWidth || height.intValue != newHeight)
		{
			((LargeTileSplitter)target).Resize(newWidth, newHeight);
		}

		if (GUILayout.Button(new GUIContent("Split")))
		{
			Split();
			serializedObject.Update();
		}

		serializedObject.ApplyModifiedProperties();
	}

	public static string GetSavePath()
	{
		string currentScene = EditorApplication.currentScene;
		
		if (currentScene == null || currentScene.Length == 0)
		{
			return "Assets";
		}
		else
		{
			return Path.GetDirectoryName(currentScene);
		}
	}

	private static Vector3 TileCenter(int x, int y, int width, int height)
	{
		Vector3 startPosition = (new Vector3(1.0f, 0.0f, 1.0f) - new Vector3(width, 0.0f, height)) * 0.5f;
		return startPosition + new Vector3(x, 0, y);
	}

	public static bool MeshNeedsSaving(Mesh mesh)
	{
		if (mesh == null)
		{
			return false;
		}

		string assetPath = AssetDatabase.GetAssetPath(mesh);

		return assetPath == null || assetPath.Length == 0;
	}

	private void SavePrefab(LargeTileSplitter splitter, Mesh renderMesh, Mesh colliderMesh, PhysicMaterial physicsMaterial, int x, int y)
	{
		UnityEngine.Object prefab = splitter.GetTargetPrefab(x, y);

		if (prefab == null)
		{
			prefab = PrefabUtility.CreateEmptyPrefab(GetSavePath() + "/" + splitter.name + "_" + x + "_" + y + ".prefab");
	    }
		else
		{
			// clear the prefab
			string preafabPath = AssetDatabase.GetAssetPath(prefab);
			UnityEngine.Object[] existingObjects = AssetDatabase.LoadAllAssetsAtPath(preafabPath);
			foreach (UnityEngine.Object existingObject in existingObjects)
			{
				if (existingObject is Mesh)
				{
					GameObject.DestroyImmediate(existingObject, true);
				}
			}
		}

		if (MeshNeedsSaving(renderMesh))
		{
			AssetDatabase.AddObjectToAsset(renderMesh, prefab);
		}
		
		if (MeshNeedsSaving(colliderMesh))
		{
			AssetDatabase.AddObjectToAsset(colliderMesh, prefab);
		}

		GameObject gameObject = new GameObject();

		if (renderMesh != null)
		{
			MeshRenderer splitterRenderer = splitter.GetComponent<MeshRenderer>();
			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = renderMesh;

			MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
			renderer.sharedMaterials = splitterRenderer.sharedMaterials;
		}

		Tile tile = gameObject.AddComponent<Tile>();
		tile.includeInRandomSelection = false;
		tile.tileType = splitter.TileType;
		tile.largeTileGroup = splitter.name;
		tile.groupX = x;
		tile.groupY = y;

		if (colliderMesh != null)
		{
			MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
			meshCollider.sharedMesh = colliderMesh;
			meshCollider.sharedMaterial = physicsMaterial;
		}

		for (int i = 0; i < splitter.transform.childCount; ++i)
		{
			GameObject child = splitter.transform.GetChild(i).gameObject;
			Vector3 childPosition = child.transform.localPosition;

			int childX = Mathf.Clamp(Mathf.FloorToInt(childPosition.x + splitter.Width * 0.5f), 0, splitter.Width - 1);
			int childY = Mathf.Clamp(Mathf.FloorToInt(childPosition.z + splitter.Height * 0.5f), 0, splitter.Height - 1);

			if (childX == x && childY == y)
			{
				GameObject copy = (GameObject)Instantiate(child);
				copy.transform.parent = gameObject.transform;
				copy.transform.localPosition = childPosition + new Vector3((splitter.Width - 1) * 0.5f - x, 0.0f, (splitter.Height - 1) * 0.5f - y);
				copy.transform.localRotation = child.transform.localRotation;
				copy.transform.localScale = child.transform.localScale;
			}
		}

		PrefabUtility.ReplacePrefab(gameObject, prefab, ReplacePrefabOptions.ReplaceNameBased);

		splitter.SetTargetPrefab(x, y, prefab);

		if (splitter.TargetTileset != null)
		{
			string prefabPath = AssetDatabase.GetAssetPath(prefab);
			GameObject prefabGameObject = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject;
			splitter.TargetTileset.AddTile(prefabGameObject.GetComponent<Tile>());
			EditorUtility.SetDirty(splitter.TargetTileset);
		}

		DestroyImmediate(gameObject);
	}


	private void Split()
	{
		LargeTileSplitter splitter = (LargeTileSplitter)target;
		Mesh renderMesh = splitter.GetComponent<MeshFilter>().sharedMesh;

		Mesh colliderMesh = null;
		PhysicMaterial physicMaterial = null;
		MeshCollider meshCollider = splitter.GetComponent<MeshCollider>();

		if (meshCollider != null)
		{
			colliderMesh = meshCollider.sharedMesh;
			physicMaterial = meshCollider.sharedMaterial;
		}
		else
		{
			physicMaterial = splitter.PhysicColliderMaterial;
		}

		Mesh[][] renderSplit = null;
		Mesh[][] colliderSplit = null;

		renderSplit = SplitMesh(renderMesh, splitter.Width, splitter.Height);

		if (colliderMesh != null)
		{
			colliderSplit = SplitMesh(colliderMesh, splitter.Width, splitter.Height);
		}
		else
		{
			colliderSplit = new Mesh[splitter.Width][];
		}

		for (int x = 0; x < splitter.Width; ++x)
		{
			if (colliderSplit[x] == null)
			{
				colliderSplit[x] = new Mesh[splitter.Height];
			}

			for (int y = 0; y < splitter.Height; ++y)
			{
				renderSplit[x][y].ShiftVertices(-TileCenter(x, y, splitter.Width, splitter.Height));
				renderSplit[x][y].name = renderMesh.name + "_" + x + "_" + y;

				if (colliderSplit[x][y] == null)
				{
					colliderSplit[x][y] = splitter.ColliderMesh;
				}
				else
				{
					colliderSplit[x][y].ShiftVertices(-TileCenter(x, y, splitter.Width, splitter.Height));
					colliderSplit[x][y].name = colliderMesh.name + "_" + x + "_" + y;
				}
			}
		}

		for (int x = 0; x < splitter.Width; ++x)
		{
			for (int y = 0; y < splitter.Height; ++y)
			{
				SavePrefab(splitter, renderSplit[x][y], colliderSplit[x][y], physicMaterial, x, y);
			}
		}
	}

	public static Mesh[] SplitMeshVertical(Mesh toSplit, int height)
	{
		Mesh[] result = new Mesh[height];

		for (int y = 0; y < height - 1; ++y)
		{
			float yPos = y - (height - 2) * 0.5f;
			Plane ySplit = new Plane(Vector3.forward, -yPos);
			
			Mesh[] splitResult = toSplit.Split(ySplit);
			result[y] = splitResult[1];
			toSplit = splitResult[0];
		}

		result[height - 1] = toSplit;

		return result;
	}

	public static Mesh[][] SplitMesh(Mesh toSplit, int width, int height)
	{
		Mesh[][] result = new Mesh[width][];

		for (int x = 0; x < width - 1; ++x)
		{
			float xPos = x - (width - 2) * 0.5f;
			Plane xSplit = new Plane(Vector3.right, -xPos);
			
			Mesh[] splitResult = toSplit.Split(xSplit);
			result[x] = SplitMeshVertical(splitResult[1], height);
			toSplit = splitResult[0];
		}
		
		result[width - 1] = SplitMeshVertical(toSplit, height);

		return result;
	}

	private void DrawQuad(Transform transform, Vector3 center, Color color)
	{
		Vector3[] vertices = new Vector3[4];
		vertices[0] = transform.TransformPoint(center + new Vector3(0.5f, 0.0f, 0.5f));
		vertices[1] = transform.TransformPoint(center + new Vector3(-0.5f, 0.0f, 0.5f));
		vertices[2] = transform.TransformPoint(center + new Vector3(-0.5f, 0.0f, -0.5f));
		vertices[3] = transform.TransformPoint(center + new Vector3(0.5f, 0.0f, -0.5f));
		
		Handles.DrawSolidRectangleWithOutline(vertices, color, Color.white);
	}
	
	public void OnSceneGUI()
	{
		LargeTileSplitter splitter = (LargeTileSplitter)target;

		Vector3 startPosition = (new Vector3(1.0f, 0.0f, 1.0f) - new Vector3(splitter.Width, 0.0f, splitter.Height)) * 0.5f;

		for (int x = 0; x < splitter.Width; ++x)
		{
			for (int y = 0; y < splitter.Height; ++y)
			{
				DrawQuad(splitter.transform, startPosition + new Vector3(x, 0, y), new Color(0.1f, 0.7f, 0.3f, 0.0f));
			}
		}
	}
}
