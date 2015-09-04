﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CustomFont : ScriptableObject {
	[System.Serializable]
	public class FontCharacter
	{
		public string character;
		public Sprite sprite;
	}
	
	public List<FontCharacter> characters = new List<FontCharacter>();

	private Dictionary<char, FontCharacter> characterMapping;

	private void CheckMapping()
	{
		if (characterMapping == null)
		{
			characterMapping = new Dictionary<char, FontCharacter>();

			foreach (FontCharacter character in characters)
			{
				characterMapping[character.character.ToCharArray()[0]] = character;
			}
		}
	}

	public float PixelsPerUnit
	{
		get
		{
			if (characters.Count > 0)
			{
				return characters[0].sprite.pixelsPerUnit;
			}
			else
			{
				return 100.0f;
			}
		}
	}

	public FontCharacter GetCharacter(char character)
	{
		CheckMapping();

		if (characterMapping.ContainsKey(character))
		{
			return characterMapping[character];
		}
		else
		{
			return null;
		}
	}

	public delegate void ForeachGlyphCallback(FontCharacter glyph);

	public void ForeachGlyph(string text, ForeachGlyphCallback callbcak)
	{
		foreach (char character in text)
		{
			FontCharacter glyph = GetCharacter(character);
			
			if (glyph != null)
			{
				callbcak(glyph);
			}
		}
	}


#if UNITY_EDITOR
	[MenuItem ("Assets/Create/Custom Font")]
	static void CreateFont()
	{
		CustomFont newFont = ScriptableObject.CreateInstance<CustomFont>();
		
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
		if (path == "")
		{
			
		}
		else if (Path.GetExtension(path) != "")
		{
			path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
		}
		
		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath (path + "/New Custom Font.asset");
		
		AssetDatabase.CreateAsset(newFont, assetPathAndName);
		AssetDatabase.SaveAssets();
		EditorUtility.FocusProjectWindow();
		Selection.activeObject = newFont;
	}
#endif
}