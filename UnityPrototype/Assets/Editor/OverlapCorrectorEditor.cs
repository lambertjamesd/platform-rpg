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

	public void OnSceneGUI()
	{	
		if (Event.current.type == EventType.MouseMove)
		{
			Event mouseEvent = Event.current;
			Ray worldRay = Camera.current.ScreenPointToRay(new Vector3(mouseEvent.mousePosition.x, Camera.current.pixelHeight - mouseEvent.mousePosition.y, 0.0f));
			TilemapOverlapCorrecter tilemap = (TilemapOverlapCorrecter)target;
			tilemap.PickerRay = ColliderMath.InverseTransformRay(worldRay, tilemap.transform);
		}
	}
}
