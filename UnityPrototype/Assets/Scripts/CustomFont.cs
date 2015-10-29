using UnityEngine;
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

		public FontCharacter(string character, Sprite sprite)
		{
			this.character = character;
			this.sprite = sprite;
		}

		public float WorldWidth
		{
			get
			{
				return sprite.rect.width / sprite.pixelsPerUnit;
			}
		}
	}
	
	public List<FontCharacter> characters = new List<FontCharacter>();

	public bool upperCaseOnly = false;

	private Dictionary<char, FontCharacter> characterMapping;

	private void CheckMapping()
	{
		if (characterMapping == null)
		{
			characterMapping = new Dictionary<char, FontCharacter>();

			foreach (FontCharacter character in characters)
			{
				char fontChar = character.character.ToCharArray()[0];

				if(character.character == "__")
				{
					fontChar = ' ';
				}

				characterMapping[fontChar] = character;
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

	public float Height
	{
		get
		{
			if (characters.Count > 0)
			{
				return characters[0].sprite.rect.height / characters[0].sprite.pixelsPerUnit;
			}
			else
			{
				return 0.0f;
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

	public delegate void ForeachGlyphCallback(FontCharacter glyph, int index);

	public void ForeachGlyph(string text, ForeachGlyphCallback callbcak)
	{
		int index = 0;
		foreach (char character in text)
		{
			FontCharacter glyph = GetCharacter(upperCaseOnly ? char.ToUpper(character) : character);
			
			if (glyph != null)
			{
				callbcak(glyph, index);
			}
			else if (character == ' ')
			{

			}

			++index;
		}
	}

	public float MeasureWidth(string text)
	{
		float result = 0.0f;

		ForeachGlyph(text, (glyph, index) => result += glyph.sprite.rect.width / glyph.sprite.pixelsPerUnit);

		return result;
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
