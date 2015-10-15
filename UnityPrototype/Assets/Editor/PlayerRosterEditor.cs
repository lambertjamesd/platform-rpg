using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

[CustomEditor(typeof(PlayerRoster))]
public class PlayerRosterEditor : Editor {
	public override void OnInspectorGUI ()
	{
		base.OnInspectorGUI ();
	}
	
	[MenuItem ("Assets/Create/Player Roster")]
	static void CreateFont()
	{
		PlayerRoster newRoster = ScriptableObject.CreateInstance<PlayerRoster>();
		
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
		if (path == "")
		{
			
		}
		else if (Path.GetExtension(path) != "")
		{
			path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
		}
		
		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath (path + "/New Player Roster.asset");
		
		AssetDatabase.CreateAsset(newRoster, assetPathAndName);
		AssetDatabase.SaveAssets();
		EditorUtility.FocusProjectWindow();
		Selection.activeObject = newRoster;
	}
}
