using UnityEngine;
using System.Collections;

public class CharacterDescription : MonoBehaviour {
	public float descriptionMargin;
	public float descriptionWidth = 5.0f;
	public float descriptionScale = 0.5f;
	public Color descriptionColor = Color.blue;
	public CustomFontRenderer customFontRenderer;

	private Player selectedPlayer;

	public void Start()
	{

	}

	public void SolidRect(Rect rect, Color color)
	{

	}

	public Player SelectedPlayer
	{
		get
		{
			return selectedPlayer;
		}

		set
		{
			selectedPlayer = value;
		}
	}

	public void Update()
	{
		if (selectedPlayer != null)
		{
			customFontRenderer.renderScale = 1.0f;
			customFontRenderer.DrawText(transform.position, selectedPlayer.name);
			customFontRenderer.renderScale = descriptionScale;

			customFontRenderer.DrawMultilineText(
				transform.position - Vector3.up * customFontRenderer.font.Height * 2.0f, 
				descriptionWidth, 
				selectedPlayer.description, 
				0.0f,
				index => new CustomFontRenderer.GlyphVariation(Vector3.zero, descriptionColor, 1.0f)
			);
		}
	}
}
