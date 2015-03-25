using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(PlaceableObject))]
public class PlaceableObjectEditor : Editor {

	private string errorMessage;

	public override void OnInspectorGUI ()
	{
		if (PrefabUtility.GetPrefabType(target) == PrefabType.Prefab)
		{
			if (GUILayout.Button("Add to Tileset"))
			{
				Tileset tileset = (Tileset)serializedObject.FindProperty("targetTileset").objectReferenceValue;

				if (tileset == null)
				{
					errorMessage = "Select a tileset to add the object to";
				}
				else
				{
					errorMessage = null;
					tileset.AddPlaceableObject((PlaceableObject)target);
					EditorUtility.SetDirty(tileset);
				}
			}

			if (errorMessage != null)
			{
				EditorGUILayout.LabelField(errorMessage);
			}
		}

		base.OnInspectorGUI ();
	}
}
