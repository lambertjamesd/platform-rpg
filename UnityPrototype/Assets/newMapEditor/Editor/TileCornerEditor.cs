using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(TileCorner))]
public class TileCornerEditor : Editor {

	bool isValid = false;

	public void OnEnable()
	{
		isValid = ((TileCorner)target).IsValidCornerSignature;
	}

	public override void OnInspectorGUI ()
	{
		if (PrefabUtility.GetPrefabType(target) == PrefabType.Prefab && GUILayout.Button("Add to tileset"))
		{
			Tileset tileset = (Tileset)serializedObject.FindProperty("targetTileset").objectReferenceValue;

			if (tileset != null)
			{
				tileset.AddCorner((TileCorner)target);
				EditorUtility.SetDirty(tileset);
			}
		}

		serializedObject.Update();

		SerializedProperty targetTileset = serializedObject.FindProperty("targetTileset");
		EditorGUILayout.PropertyField(targetTileset);

		SerializedProperty edgeAngles = serializedObject.FindProperty("edgeAngles");

		int newLength = Mathf.Max(2, EditorGUILayout.IntField("Edge Angle Count", Mathf.Max(edgeAngles.arraySize, 1) - 1));

		if (newLength != edgeAngles.arraySize - 1)
		{
			serializedObject.ApplyModifiedProperties();

			TileCorner corner = (TileCorner)target;

			corner.EdgeAngleCount = newLength;

			serializedObject.Update();
		}

		for (int i = 0; i < newLength; ++i)
		{
			SerializedProperty edgeType = edgeAngles.GetArrayElementAtIndex(i);

			edgeType.intValue = (int)(EdgeAngle)EditorGUILayout.EnumPopup("Edge Angle", (EdgeAngle)edgeType.enumValueIndex);
		}
		
		SerializedProperty tileTypes = serializedObject.FindProperty("tileTypes");

		for (int i = 0; tileTypes != null && i < tileTypes.arraySize; ++i)
		{
			GUI.enabled = false;
			EditorGUILayout.ColorField(TileColor(i, newLength, isValid));
			GUI.enabled = true;
			TileEdgeEditor.TileType("Tile Type", tileTypes.GetArrayElementAtIndex(i));
		}

		serializedObject.ApplyModifiedProperties();

		TileCorner tileCorner = (TileCorner)target;
     	tileCorner.RecalcFinalEdge();
     	isValid = tileCorner.IsValidCornerSignature;
	}

	private Color TileColor(int index, int edgeCount, bool isValid)
	{
		Color blendTo = isValid ? new Color(0.0f, 1.0f, 0.0f, 0.1f) : new Color(1.0f, 0.0f, 0.0f, 0.1f);
		return Color.Lerp(new Color(0.0f, 0.0f, 0.0f, 0.1f), blendTo, (float)index / edgeCount);
	}
	
	public void OnSceneGUI()
	{
		TileCorner corner = (TileCorner)target;
		Quaternion currentOrienation = corner.transform.rotation;
		Vector3 pivotPoint = corner.transform.position;

		DrawQuad(pivotPoint, currentOrienation, TileColor(0, corner.EdgeAngleCount, isValid));

		for (int i = 0; i < corner.EdgeAngleCount; ++i)
		{
			currentOrienation = currentOrienation * TileCorner.StepRotation(corner.GetEdgeAngle(i));
			DrawQuad(pivotPoint, currentOrienation, TileColor(i + 1, corner.EdgeAngleCount, isValid));
		}
	}

	private void DrawQuad(Vector3 pivotPoint, Quaternion orientation, Color color)
	{
		Vector3[] vertices = new Vector3[]{
			pivotPoint,
			pivotPoint + orientation * new Vector3(0.0f, 0.0f, -1.0f),
			pivotPoint + orientation * new Vector3(-1.0f, 0.0f, -1.0f),
			pivotPoint + orientation * new Vector3(-1.0f, 0.0f, 0.0f)
		};

		Handles.DrawSolidRectangleWithOutline(vertices, color, Color.black);
	}
}
