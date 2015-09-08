using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(CustomFont))]
public class NewBehaviourScript : Editor {
	public override void OnInspectorGUI ()
	{
		base.OnInspectorGUI ();

		string fontName = ((CustomFont)target).name;

		if (GUILayout.Button("Auto Populate"))
		{
			HashSet<string> filesToLoad = new HashSet<string>(
				AssetDatabase.FindAssets(fontName + " t:Sprite").Select(guid => AssetDatabase.GUIDToAssetPath(guid))
			);

			((CustomFont)target).characters = filesToLoad.SelectMany(filename => 
				AssetDatabase.LoadAllAssetsAtPath(filename)
			                       .Where(maybeSprite => maybeSprite is Sprite)
			                       .Select(sprite => (Sprite)sprite)
			                       .Where(sprite => sprite.name.StartsWith(fontName))
			).Select(sprite => new CustomFont.FontCharacter(sprite.name.Substring(fontName.Length), sprite)).ToList();
		}
	}
}
