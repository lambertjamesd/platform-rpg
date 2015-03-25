using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(MeshSimplifyTest))]
public class MeshSimplifyTestEditor : Editor {

	private bool positionOnly = false;

	public override void OnInspectorGUI ()
	{
		base.OnInspectorGUI();
		
		positionOnly = EditorGUILayout.Toggle("Position Only", positionOnly);

		if (GUILayout.Button("Simplify Mesh"))
		{
			MeshSimplifyTest meshSimplify = (MeshSimplifyTest)target;

			if (positionOnly)
			{
				MeshCollider filterTest = meshSimplify.GetComponent<MeshCollider>();
				filterTest.sharedMesh = filterTest.sharedMesh.Simplify(positionOnly);
			}
			else
			{
				MeshFilter filterTest = meshSimplify.GetComponent<MeshFilter>();
				filterTest.sharedMesh = filterTest.sharedMesh.Simplify(positionOnly);
			}
		}
	}
}
