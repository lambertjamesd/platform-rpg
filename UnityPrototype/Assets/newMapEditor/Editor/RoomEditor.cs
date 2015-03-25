using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

[CustomEditor(typeof(Room))]
public class RoomEditor : Editor
{
	[MenuItem("Assets/Create/Room")]
	public static void CreateLevelData() {
		Room newRoom = ScriptableObject.CreateInstance<Room>();
		string filename = AssetDatabase.GetAssetPath(Selection.activeObject);

		if (filename != "" && Path.GetExtension(filename) != "")
		{
			filename = Path.GetDirectoryName(filename);
		}
		
		filename = AssetDatabase.GenerateUniqueAssetPath(filename + "/New Room.asset");

		AssetDatabase.CreateAsset(newRoom, filename);
		AssetDatabase.SaveAssets();
		EditorUtility.FocusProjectWindow();
		Selection.activeObject = newRoom;
	}
}
