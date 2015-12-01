using UnityEngine;
using System.Collections;

public class CharacterSelectionTile : MonoBehaviour {

	private int userMaterialIndex = 0;
	private int selectionMaterialIndex = 2;

	private Player displayedCharacter;
	private Renderer tileRenderer;
	private bool selected;
	private bool disabled;

	private Material userTexture;
	private Material selectionMaterial;

	public void Awake()
	{
		tileRenderer = GetComponent<Renderer>();

		Material[] materials = tileRenderer.materials;
		userTexture = materials[userMaterialIndex];
		selectionMaterial = materials[selectionMaterialIndex];

		Selected = false;
		Disabled = false;
	}

	public Player DisplayedCharacter
	{
		get
		{
			return displayedCharacter;
		}

		set
		{
			displayedCharacter = value;
			userTexture.mainTexture = displayedCharacter.Settings.characterImage;
		}
	}

	public bool Selected
	{
		get
		{
			return selected;
		}

		set
		{
			selected = value;
			selectionMaterial.color = selected ? Color.green : Color.clear;
		}
	}

	public bool Disabled
	{
		get
		{
			return disabled;
		}

		set
		{
			disabled = value;
			userTexture.SetFloat("_Saturation", disabled ? 0.0f : 1.0f);
		}
	}
}
