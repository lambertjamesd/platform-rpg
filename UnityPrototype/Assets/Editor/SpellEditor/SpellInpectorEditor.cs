using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(EffectAsset))]
public class SpellInpectorEditor : Editor {

	bool expandPrefabs = false;

	public override void OnInspectorGUI ()
	{
		EffectAsset effect = (EffectAsset)target;
		if (GUILayout.Button("Edit"))
		{
			SpellEditor editor = EditorWindow.GetWindow<SpellEditor>();
			editor.CurretlyOpenSpell = (EffectAsset)target;
		}

		expandPrefabs = EditorGUILayout.Foldout(expandPrefabs, "Prefabs");

		if (expandPrefabs)
		{
			for (int i = 0; i < effect.prefabList.Count; ++i)
			{
				effect.prefabList[i] = EditorGUILayout.ObjectField(effect.prefabList[i], typeof(GameObject), false) as GameObject;
			}
		}

		EditorGUILayout.SelectableLabel(effect.xmlText, GUILayout.ExpandHeight(true));
	}

	public void OnEnable()
	{

	}
}
