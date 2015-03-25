using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

[CustomEditor(typeof(SpellDescription))]
public class SpellDescriptionEditor : Editor {
	public override void OnInspectorGUI ()
	{
		base.OnInspectorGUI ();
	}
	
	[MenuItem ("Assets/Create/Spell Description")]
	static void CreateSpell()
	{
		SpellDescription newSpell = ScriptableObject.CreateInstance<SpellDescription>();
		
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
		if (path == "")
		{
			
		}
		else if (Path.GetExtension(path) != "")
		{
			path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
		}
		
		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath (path + "/New Spell Description.asset");
		
		AssetDatabase.CreateAsset(newSpell, assetPathAndName);
		AssetDatabase.SaveAssets();
		EditorUtility.FocusProjectWindow();
		Selection.activeObject = newSpell;
	}
}
