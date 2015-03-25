using UnityEngine;
using System.Collections;

public class PlayerHUD : MonoBehaviour {

	public Texture2D whitePixel;
	public Texture playerIconHUD;
	public Rect iconPosition;
	public Rect statsPosition;
	public float statLineHeight;

	public Texture spellIconHUD;
	public Rect firstIconPosition;
	public Vector2 iconOffset;

	public Vector2 hoverDescriptionSize;
	public float hoverTitleHeight;
	public float lineHeight;

	private SpellDescription currentSpell;
	private Vector2 spellHoverPoint;

	private Player currentPlayer;

	GUIStyle centeredStyle;

	public void Start()
	{
		whitePixel = new Texture2D(1, 1);
		whitePixel.SetPixel(0, 0, Color.white);
		whitePixel.Apply();
	}

	public Player CurrentPlayer
	{
		get
		{
			return currentPlayer;
		}

		set
		{
			currentPlayer = value;
		}
	}

	private Rect SpellHUDLocation
	{
		get
		{
			return new Rect(Mathf.Floor((Screen.width - spellIconHUD.width) * 0.5f), Screen.height - spellIconHUD.height, spellIconHUD.width, spellIconHUD.height);
		}
	}

	public void SolidRect(Rect rect, Color color)
	{
		Color lastColor = GUI.color;
		GUI.color = color;
		GUI.DrawTexture(rect, whitePixel);
		GUI.color = lastColor;
	}

	public void OnGUI()
	{
		if (centeredStyle == null)
		{
			centeredStyle = new GUIStyle(GUI.skin.label);
			centeredStyle.alignment = TextAnchor.MiddleCenter;
		}

		GUI.BeginGroup(new Rect(0.0f, Screen.height - playerIconHUD.height, playerIconHUD.width, playerIconHUD.height));
		GUI.DrawTexture(new Rect(0.0f, 0.0f, playerIconHUD.width, playerIconHUD.height), playerIconHUD);

		if (currentPlayer != null)
		{
			GUI.DrawTexture(iconPosition, currentPlayer.settings.characterImage);

			Rect currentStatRect = statsPosition;
			currentStatRect.height = statLineHeight;
			GUI.color = Color.white;
			GUI.Label(currentStatRect, "Speed: " + currentPlayer.Stats.GetNumberStat("maxMoveSpeed")); currentStatRect.y += statLineHeight;
			GUI.Label(currentStatRect, "Ajility: " + currentPlayer.Settings.moveAcceleration); currentStatRect.y += statLineHeight;
			GUI.Label(currentStatRect, "Jump Height: " + currentPlayer.Stats.GetNumberStat("maxJumpHeight")); currentStatRect.y += statLineHeight;
		}

		GUI.EndGroup();

		GUI.BeginGroup(SpellHUDLocation);
		GUI.DrawTexture(new Rect(0.0f, 0.0f, spellIconHUD.width, spellIconHUD.height), spellIconHUD);

		if (currentPlayer != null)
		{
			Rect currentSpellIconRect = firstIconPosition;

			for (int i = 0; i < currentPlayer.GetSpellCount(); ++i)
			{
				GUI.DrawTexture(currentSpellIconRect, currentPlayer.GetSpell(i).icon);

				float cooldown = currentPlayer.GetSpellCooldown(i);

				if (cooldown > 0)
				{
					SolidRect(currentSpellIconRect, new Color(0.0f, 0.0f, 0.0f,0.5f));
					GUI.color = Color.white;
					string cooldownString = cooldown < 1.0f ? cooldown.ToString("0.0") : cooldown.ToString("0");
					GUI.Label(currentSpellIconRect, cooldownString, centeredStyle);
				}
				
				currentSpellIconRect.x += iconOffset.x;
				currentSpellIconRect.y += iconOffset.y;
			}
		}

		GUI.EndGroup();
	}
}
