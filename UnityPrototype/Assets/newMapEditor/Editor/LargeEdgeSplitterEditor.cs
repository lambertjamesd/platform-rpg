using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(LargeEdgeSplitter))]
public class LargeEdgeSplitterEditor : Editor {

	public override void OnInspectorGUI ()
	{
		serializedObject.Update();

		SerializedProperty tilesetProperty = serializedObject.FindProperty("targetTileset");
		tilesetProperty.objectReferenceValue = EditorGUILayout.ObjectField("Target Tileset", tilesetProperty.objectReferenceValue, typeof(Tileset), false);
		
		SerializedProperty edgeType = serializedObject.FindProperty("edgeType");
		edgeType.stringValue = EditorGUILayout.TextField("Edge Type", edgeType.stringValue);
		
		SerializedProperty width = serializedObject.FindProperty("width");
		int newWidth = width.intValue;

		EditorGUILayout.PropertyField(serializedObject.FindProperty("reverseIndices"));

		if (newWidth == 0)
		{
			LargeEdgeSplitter splitter = (LargeEdgeSplitter)target;
			MeshFilter meshFilter = splitter.GetComponent<MeshFilter>();
			
			if (meshFilter != null && meshFilter.sharedMesh != null)
			{
				Bounds meshBounds = meshFilter.sharedMesh.bounds;
				newWidth = Mathf.FloorToInt(meshBounds.size.z + 0.5f);
			}
		}

		newWidth = Mathf.Max(1, EditorGUILayout.IntField("Width", newWidth));
		
		if (width.intValue != newWidth)
		{
			((LargeEdgeSplitter)target).Resize(newWidth);
		}

		SerializedProperty usedEdgeAngles = serializedObject.FindProperty("usedEdgeAngles");
		
		EditorGUILayout.LabelField("Used Edge Angles");
		
		SerializedProperty wallAngle = usedEdgeAngles.GetArrayElementAtIndex(0);
		SerializedProperty flatAngle = usedEdgeAngles.GetArrayElementAtIndex(1);
		SerializedProperty cliffAngle = usedEdgeAngles.GetArrayElementAtIndex(2);
		
		wallAngle.boolValue = EditorGUILayout.Toggle("Wall", wallAngle.boolValue);
		flatAngle.boolValue = EditorGUILayout.Toggle("Flat", flatAngle.boolValue);
		cliffAngle.boolValue = EditorGUILayout.Toggle("Cliff", cliffAngle.boolValue);
		
		TileEdgeEditor.TileType("Tile Type A", serializedObject.FindProperty("tileTypeA"), serializedObject.FindProperty("typeASides"));
		TileEdgeEditor.TileType("Tile Type B", serializedObject.FindProperty("tileTypeB"), serializedObject.FindProperty("typeBSides"));
		
		serializedObject.ApplyModifiedProperties();

		if (GUILayout.Button("Split"))
		{
			Split();
		}
	}
	
	public void OnSceneGUI()
	{
		LargeEdgeSplitter splitter = (LargeEdgeSplitter)target;


		for (int i = 1; i < splitter.Width; ++i)
		{
			float offset = i - splitter.Width * 0.5f;

			Vector3 pointA = splitter.transform.TransformPoint(new Vector3(-1.0f, 0.0f, offset));
			Vector3 pointB = splitter.transform.TransformPoint(new Vector3(1.0f, 0.0f, offset));

			Handles.DrawLine(pointA, pointB);
		}
	}
	
	
	private void SavePrefab(LargeEdgeSplitter splitter, Mesh renderMesh, Mesh colliderMesh, PhysicMaterial physicsMaterial, int x)
	{
		UnityEngine.Object prefab = splitter.GetTargetPrefab(x);
		
		if (prefab == null)
		{
			prefab = PrefabUtility.CreateEmptyPrefab(LargeTileSplitterEditor.GetSavePath() + "/" + splitter.name + "_" + x + ".prefab");
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
		
		if (LargeTileSplitterEditor.MeshNeedsSaving(renderMesh))
		{
			AssetDatabase.AddObjectToAsset(renderMesh, prefab);
		}
		
		if (LargeTileSplitterEditor.MeshNeedsSaving(colliderMesh))
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
		
		TileEdge tileEdge = gameObject.AddComponent<TileEdge>();
		tileEdge.EdgeType = splitter.EdgeType;
		tileEdge.GroupIndex = splitter.ReverseIndices ? splitter.Width - x - 1 : x;
		tileEdge.GroupSize = splitter.Width;

		tileEdge.TileTypeA = splitter.TileTypeA;
		tileEdge.TileTypeB = splitter.TileTypeB;

		for (int i = 0; i < 3; ++i)
		{
			EdgeAngle edgeAngle = (EdgeAngle)i;
			tileEdge.SetUseEdgeAngle(edgeAngle, splitter.DoesUseEdgeAngle(edgeAngle));
		}

		for (int i = 0; i < 4; ++i)
		{
			TileSide tileSide = (TileSide)i;
			tileEdge.SetUseTypeASide(tileSide, splitter.DoesUseTypeASide(tileSide));
			tileEdge.SetUseTypeBSide(tileSide, splitter.DoesUseTypeBSide(tileSide));
		}
		
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

			int childX = Mathf.Clamp(Mathf.FloorToInt(childPosition.z + splitter.Width * 0.5f), 0, splitter.Width - 1);
			
			if (childX == x)
			{
				GameObject copy = (GameObject)Instantiate(child);
				copy.name = child.name;
				copy.transform.parent = gameObject.transform;
				copy.transform.localPosition = childPosition + Vector3.forward * ((splitter.Width - 1) * 0.5f - x);
				copy.transform.localRotation = child.transform.localRotation;
				copy.transform.localScale = child.transform.localScale;
			}
		}
		
		PrefabUtility.ReplacePrefab(gameObject, prefab, ReplacePrefabOptions.ReplaceNameBased);
		
		splitter.SetTargetPrefab(x, prefab);

		if (splitter.TargetTileset != null)
		{
			string prefabPath = AssetDatabase.GetAssetPath(prefab);
			GameObject prefabGameObject = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject;
			splitter.TargetTileset.AddTileEdge(prefabGameObject.GetComponent<TileEdge>());
			EditorUtility.SetDirty(splitter.TargetTileset);
		}
		
		DestroyImmediate(gameObject);
	}

	private void Split()
	{
		LargeEdgeSplitter splitter = (LargeEdgeSplitter)target;
		Mesh renderMesh = splitter.GetComponent<MeshFilter>().sharedMesh;
		
		Mesh colliderMesh = null;
		PhysicMaterial physicMaterial = null;
		MeshCollider meshCollider = splitter.GetComponent<MeshCollider>();
		
		if (meshCollider != null)
		{
			colliderMesh = meshCollider.sharedMesh;
			physicMaterial = meshCollider.sharedMaterial;
		}
		
		Mesh[] renderSplit = renderMesh == null ? new Mesh[splitter.Width] : LargeTileSplitterEditor.SplitMeshVertical(renderMesh, splitter.Width);
		Mesh[] colliderSplit = colliderMesh == null ? new Mesh[splitter.Width] : LargeTileSplitterEditor.SplitMeshVertical(colliderMesh, splitter.Width);
		
		for (int x = 0; x < splitter.Width; ++x)
		{
			Vector3 shiftAmount = new Vector3(0.0f, 0.0f, (splitter.Width - 1.0f) * 0.5f - x);

			if (renderSplit[x] != null)
			{
				renderSplit[x].ShiftVertices(shiftAmount);
			}

			if (colliderSplit[x] != null)
			{	
				colliderSplit[x].ShiftVertices(shiftAmount);
			}

			SavePrefab(splitter, renderSplit[x], colliderSplit[x], physicMaterial, x);
		}
	}
}
