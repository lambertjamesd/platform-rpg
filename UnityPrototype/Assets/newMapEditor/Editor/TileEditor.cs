using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(Tile))]
public class TileEditor : Editor
{
	public override void OnInspectorGUI ()
	{
		if (PrefabUtility.GetPrefabType(target) == PrefabType.Prefab && GUILayout.Button("Add to Tileset"))
		{
			Tileset addTo = (Tileset)serializedObject.FindProperty("targetTileset").objectReferenceValue;
			
			if (addTo != null)
			{
				addTo.AddTile((Tile)target);
				EditorUtility.SetDirty(addTo);
			}
		}

		base.OnInspectorGUI ();
	}
}
