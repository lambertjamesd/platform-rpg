using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(TilemapOverlapCorrecter))]
public class OverlapCorrectorEditor : Editor {

	public override void OnInspectorGUI ()
	{
		base.OnInspectorGUI ();

		if (GUILayout.Button("Rebuild collider"))
		{
			((TilemapOverlapCorrecter)target).Rebuild();
		}
	}
}
