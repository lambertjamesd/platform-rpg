using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(TileEdge))]
public class TileEdgeEditor : Editor
{
	public static void TileType(string name, SerializedProperty tileTypeInfo)
	{
		SerializedProperty tileType = tileTypeInfo.FindPropertyRelative("tileType");
		SerializedProperty sides = tileTypeInfo.FindPropertyRelative("tileSides");

		tileType.stringValue = EditorGUILayout.TextField(name, tileType.stringValue);
		
		SerializedProperty right = sides.GetArrayElementAtIndex(0);
		SerializedProperty bottom = sides.GetArrayElementAtIndex(1);
		SerializedProperty left = sides.GetArrayElementAtIndex(2);
		SerializedProperty top = sides.GetArrayElementAtIndex(3);

		EditorGUILayout.BeginVertical();
		right.boolValue = EditorGUILayout.Toggle("Right", right.boolValue);
		bottom.boolValue = EditorGUILayout.Toggle("Bottom", bottom.boolValue);
		left.boolValue = EditorGUILayout.Toggle("Left", left.boolValue);
		top.boolValue = EditorGUILayout.Toggle("Top", top.boolValue);
		EditorGUILayout.EndVertical();

		EditorGUILayout.PropertyField(tileTypeInfo.FindPropertyRelative("replaceTile"));
	}

	public static void TileType(string name, SerializedProperty tileType, SerializedProperty sides)
	{
		tileType.stringValue = EditorGUILayout.TextField(name, tileType.stringValue);

		SerializedProperty right = sides.GetArrayElementAtIndex(0);
		SerializedProperty bottom = sides.GetArrayElementAtIndex(1);
		SerializedProperty left = sides.GetArrayElementAtIndex(2);
		SerializedProperty top = sides.GetArrayElementAtIndex(3);
		
		right.boolValue = EditorGUILayout.Toggle("Right", right.boolValue);
		bottom.boolValue = EditorGUILayout.Toggle("Bottom", bottom.boolValue);
		left.boolValue = EditorGUILayout.Toggle("Left", left.boolValue);
		top.boolValue = EditorGUILayout.Toggle("Top", top.boolValue);
	}

	public override void OnInspectorGUI ()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField(serializedObject.FindProperty("targetTileset"));
		
		if (PrefabUtility.GetPrefabType(target) == PrefabType.Prefab && GUILayout.Button("Add to Tileset"))
		{
			Tileset addTo = (Tileset)serializedObject.FindProperty("targetTileset").objectReferenceValue;
			
			if (addTo != null)
			{
				addTo.AddTileEdge((TileEdge)target);
				EditorUtility.SetDirty(addTo);
			}
		}

		SerializedProperty edgeType = serializedObject.FindProperty("edgeType");
		edgeType.stringValue = EditorGUILayout.TextField("Edge Type", edgeType.stringValue);
		
		SerializedProperty groupIndex = serializedObject.FindProperty("groupIndex");
		groupIndex.intValue = EditorGUILayout.IntField("Group Index", groupIndex.intValue);

		SerializedProperty groupSize = serializedObject.FindProperty("groupSize");
		groupSize.intValue = EditorGUILayout.IntField("Group Size", groupSize.intValue);

		SerializedProperty usedEdgeAngles = serializedObject.FindProperty("usedEdgeAngles");

		EditorGUILayout.LabelField("Used Edge Angles");

		SerializedProperty wallAngle = usedEdgeAngles.GetArrayElementAtIndex(0);
		SerializedProperty flatAngle = usedEdgeAngles.GetArrayElementAtIndex(1);
		SerializedProperty cliffAngle = usedEdgeAngles.GetArrayElementAtIndex(2);

		wallAngle.boolValue = EditorGUILayout.Toggle("Wall", wallAngle.boolValue);
		flatAngle.boolValue = EditorGUILayout.Toggle("Flat", flatAngle.boolValue);
		cliffAngle.boolValue = EditorGUILayout.Toggle("Cliff", cliffAngle.boolValue);
		
		TileType("Tile Type A", serializedObject.FindProperty("tileA"));
		TileType("Tile Type B", serializedObject.FindProperty("tileB"));

		serializedObject.ApplyModifiedProperties();
	}
}
