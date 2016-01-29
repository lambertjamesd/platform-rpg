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

	public Rect[] spellLocations = new Rect[3];
	public float spellDescriptionMargin = 20.0f;
	public bool showSpellDescriptions;

	private Player currentPlayer;

	private Vector2 spellDescriptionPosition;

	private IInputSource controllerInputSource = new ControllerInputSource(Camera.main.transform);

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
			GUI.Label(currentStatRect, "Speed: " + currentPlayer.Stats.GetBaseStat("maxMoveSpeed")); currentStatRect.y += statLineHeight;
			GUI.Label(currentStatRect, "Ajility: " + currentPlayer.Stats.GetBaseStat("moveAcceleration")); currentStatRect.y += statLineHeight;
			GUI.Label(currentStatRect, "Jump Height: " + currentPlayer.Stats.GetBaseStat("maxJumpHeight")); currentStatRect.y += statLineHeight;
		}

		GUI.EndGroup();

		int hoverSpellIndex = 0;
		
		GUI.BeginGroup(SpellHUDLocation);
		GUI.DrawTexture(new Rect(0.0f, 0.0f, spellIconHUD.width, spellIconHUD.height), spellIconHUD);

		if (currentPlayer != null)
		{
			controllerInputSource.FrameStart(null);
			
			for (hoverSpellIndex = 0; hoverSpellIndex < Player.SPELL_COUNT; ++hoverSpellIndex)
			{
				if (controllerInputSource.State.FireButton(hoverSpellIndex))
				{
					break;
				}
			}

			Rect currentSpellIconRect = firstIconPosition;

			for (int i = 0; i < currentPlayer.GetSpellCount(); ++i)
			{
				SpellDescription spell = currentPlayer.GetSpell(i);
				GUI.DrawTexture(currentSpellIconRect, spell.icon);

				float cooldown = currentPlayer.GetSpellCooldown(i);
				int chargeCount = currentPlayer.GetChargeCount(i);

				if (cooldown > 0 && chargeCount < spell.maxCharges)
				{
					if (chargeCount == 0)
					{
						SolidRect(currentSpellIconRect, new Color(0.0f, 0.0f, 0.0f,0.5f));
					}

					GUI.color = Color.white;
					string cooldownString = cooldown < 1.0f ? cooldown.ToString("0.0") : cooldown.ToString("0");
					GUI.Label(currentSpellIconRect, cooldownString, centeredStyle);
				}

				if (spell.maxCharges > 1)
				{
					Rect chargeRect = currentSpellIconRect;
					chargeRect.width *= 0.5f;
					chargeRect.height *= 0.5f;
					GUI.Label(chargeRect, chargeCount.ToString(), centeredStyle);
				}

				if (hoverSpellIndex < Player.SPELL_COUNT && hoverSpellIndex != i)
				{
					SolidRect(currentSpellIconRect, new Color(0.0f, 0.0f, 0.0f,0.5f));
				}
				
				currentSpellIconRect.x += iconOffset.x;
				currentSpellIconRect.y += iconOffset.y;
			}
		}

		GUI.EndGroup();

		if (currentPlayer !=null && showSpellDescriptions)
		{
			if (hoverSpellIndex < Player.SPELL_COUNT && hoverSpellIndex < spellLocations.Length)
			{
				Rect describePosition = spellLocations[hoverSpellIndex];
				describePosition.x += (Screen.width - spellIconHUD.width) * 0.5f;
				describePosition.y += Screen.height;

				GUI.BeginGroup(describePosition);
				Rect drawRect = new Rect(0.0f, 0.0f, describePosition.width, describePosition.height);
				SolidRect(drawRect, Color.white);
				GUI.color = Color.black;
				drawRect.x += spellDescriptionMargin;
				drawRect.y += spellDescriptionMargin;
				drawRect.width -= spellDescriptionMargin * 2.0f;
				drawRect.height -= spellDescriptionMargin * 2.0f;
				GUI.Label(drawRect, currentPlayer.GetSpell(hoverSpellIndex).FormattedDescription);
				GUI.EndGroup();
			}
		}
	}
}
