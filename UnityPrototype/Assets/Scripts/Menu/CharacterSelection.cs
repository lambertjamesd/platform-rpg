using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CharacterSelection : MonoBehaviour {
	public PlayerRoster roster;
	public Transform rosterPosition;
	public Transform previewPosition;
	public Transform teamAPosition;
	public Transform teamBPosition;
	public int columnCount;
	public int rowCount;
	public Vector2 tileSize;
	public int teamSize = 3;

	public CharacterSelectionTile selectionTileTemplate;
	public CharacterDescription characterDescription;

	private int activeSpellIndex = 0;
	private SpellPreview activeSpellPreview;
	private float currentPreviewDuration;

	private List<CharacterSelectionTile> playerTiles = new List<CharacterSelectionTile>();
	private CharacterSelectionTile currentSelection = null;
	
	private List<CharacterSelectionTile> teamASelection = new List<CharacterSelectionTile>();
	private List<CharacterSelectionTile> teamBSelection = new List<CharacterSelectionTile>();
	
	private int selectedRow = 0;
	private int selectedCol = 0;

	private Vector2 lastJoystickInput;

	private int currentTeamSelection = 0;

	public void Start()
	{
		rowCount = (roster.players.Count + columnCount - 1) % columnCount;

		for (int index = 0; index < roster.players.Count; ++index)
		{
			int col = index % columnCount;
			int row = index / columnCount;

			CharacterSelectionTile tile = Instantiate<CharacterSelectionTile>(selectionTileTemplate);
			tile.transform.position = rosterPosition.position + new Vector3(tileSize.x * (0.5f + col - 0.5f * columnCount), tileSize.y * row, 0.0f);
			tile.transform.parent = rosterPosition;
			tile.DisplayedCharacter = roster.players[index];

			playerTiles.Add(tile);
		}

		for (int index = 0; index < teamSize; ++index)
		{
			CharacterSelectionTile teamA = Instantiate<CharacterSelectionTile>(selectionTileTemplate);
			CharacterSelectionTile teamB = Instantiate<CharacterSelectionTile>(selectionTileTemplate);

			teamA.Disabled = true;
			teamB.Disabled = true;

			teamA.transform.position = teamAPosition.position + new Vector3(0.0f, -tileSize.y * index, 0.0f);
			teamA.transform.parent = teamAPosition;
			teamASelection.Add(teamA);

			teamB.transform.position = teamBPosition.position + new Vector3(0.0f, -tileSize.y * index, 0.0f);
			teamB.transform.parent = teamBPosition;
			teamBSelection.Add(teamB);

		}
		
		SelectFirst();
	}

	public void Update()
	{
		Vector2 joystickInput = new Vector2(
			Input.GetAxisRaw("Horizontal"),
			Input.GetAxisRaw("Vertical")
		);

		if (joystickInput.x > 0.5f && lastJoystickInput.x <= 0.5f)
		{
			StepCol(1);
		}
		else if (joystickInput.x < -0.5f && lastJoystickInput.x >= -0.5f)
		{
			StepCol(-1);
		}
		else if (joystickInput.y > 0.5f && lastJoystickInput.y <= 0.5f)
		{
			StepRow(1);
		}
		else if (joystickInput.y < -0.5f && lastJoystickInput.y >= -0.5f)
		{
			StepRow(-1);
		}

		if (Input.GetButtonDown("Select"))
		{
			ChooseSelectedTile();

			if (currentTeamSelection == 2 * teamSize)
			{
				FightSetup fightSetup = Object.FindObjectOfType<FightSetup>();

				fightSetup.SetTeamSize(2, teamSize);

				for (int i = 0; i < teamSize; ++i)
				{
					fightSetup.SetPlayer(0, i, teamASelection[i].DisplayedCharacter);
					fightSetup.SetPlayer(1, i, teamBSelection[i].DisplayedCharacter);
				}

				Object.DontDestroyOnLoad(fightSetup);
				Application.LoadLevel("FightScene");
			}
		}

		if (currentPreviewDuration > 0.0f && activeSpellPreview != null)
		{
			currentPreviewDuration -= Time.deltaTime;

			if (currentPreviewDuration <= 0.0f)
			{
				ShowSpellPreview();
			}
		}

		lastJoystickInput = joystickInput;
	}

	private void StepCol(int dirCol, bool recurse = true)
	{
		int startPos = selectedCol;

		do
		{
			selectedCol = (selectedCol + dirCol + columnCount) % columnCount;
		} while (startPos != selectedCol && !IsValidSelection(selectedRow, selectedCol));

		if (startPos == selectedCol)
		{
			do
			{
				selectedCol = (selectedCol + dirCol + columnCount) % columnCount;
				
				if (recurse && !IsValidSelection(selectedRow, selectedCol))
				{
					StepRow(1, false);
				}
				
			} while (startPos != selectedCol && !IsValidSelection(selectedRow, selectedCol));
		}

		SetSelectedTile(selectedRow, selectedCol);
	}

	private void StepRow(int dirRow, bool recurse = true)
	{
		int startPos = selectedRow;

		do
		{
			selectedRow = (selectedRow + dirRow + rowCount) % rowCount;

		} while (startPos != selectedRow && !IsValidSelection(selectedRow, selectedCol));

		if (startPos == selectedRow)
		{
			do
			{
				selectedRow = (selectedRow + dirRow + rowCount) % rowCount;
				
				if (recurse && !IsValidSelection(selectedRow, selectedCol))
				{
					StepCol(1, false);
				}
				
			} while (startPos != selectedRow && !IsValidSelection(selectedRow, selectedCol));
		}

		SetSelectedTile(selectedRow, selectedCol);
	}

	private void SelectFirst()
	{
		for (int i = 0; i < roster.players.Count; ++i)
		{
			if (!playerTiles[i].Disabled)
			{
				SetSelectedTile(i / columnCount, i % columnCount);
				break;
			}
		}
	}

	private bool IsValidSelection(int row, int col)
	{
		CharacterSelectionTile tile = GetTile(row, col);
		return tile != null && !tile.Disabled;
	}
	
	private CharacterSelectionTile GetTile(int row, int col)
	{
		int index = row * columnCount + col;
		if (row >= 0 && row < rowCount && col >= 0 && col < columnCount && index < playerTiles.Count)
		{
			return playerTiles[index];
		}
		else
		{
			return null;
		}
	}

	private CharacterSelectionTile GetSelectedTile()
	{
		return GetTile(selectedRow, selectedCol);
	}

	private void SetSelectedTile(int row, int col)
	{
		selectedRow = row;
		selectedCol = col;

		if (currentSelection != null)
		{
			currentSelection.Selected = false;
		}

		currentSelection = GetTile(row, col);

		if (currentSelection != null)
		{
			currentSelection.Selected = true;
			characterDescription.SelectedPlayer = currentSelection.DisplayedCharacter;
			activeSpellIndex = 0;
			ShowSpellPreview();
		}
	}

	private void ChooseSelectedTile()
	{
		int team = CurrentSelectingTeam(currentTeamSelection);

		List<CharacterSelectionTile> teamRoster = team == 0 ? teamASelection : teamBSelection;

		for (int i = 0; i < teamRoster.Count; ++i)
		{
			if (teamRoster[i].Disabled)
			{
				teamRoster[i].Disabled = false;
				teamRoster[i].DisplayedCharacter = currentSelection.DisplayedCharacter;
				currentSelection.Disabled = true;
				break;
			}
		}

		++currentTeamSelection;

		if (currentTeamSelection != teamSize * 2)
		{
			SelectFirst();
		}
		else
		{
			currentSelection.Selected = false;
		}
	}

	private static int CurrentSelectingTeam(int selectionIndex)
	{
		return ((selectionIndex + 1) / 2) % 2;
	}

	private void ShowSpellPreview()
	{
		if (activeSpellPreview != null)
		{
			Destroy(activeSpellPreview.gameObject);
			activeSpellPreview = null;
		}

		int startIndex = activeSpellIndex;
		CharacterSelectionTile selectedTile = GetSelectedTile();
		SpellPreview nextPreview = null;

		if (!selectedTile.Disabled)
		{
			Player currentPlayer = selectedTile.DisplayedCharacter;
			SpellCaster caster = currentPlayer.gameObject.GetComponent<SpellCaster>();

			do
			{
				nextPreview = caster.GetSpell(activeSpellIndex).spellPreview;
				activeSpellIndex = (activeSpellIndex + 1) % caster.GetSpellCount();
			} while (startIndex != activeSpellIndex && nextPreview == null);
		}

		if (nextPreview)
		{
			activeSpellPreview = Instantiate<SpellPreview>(nextPreview);
			activeSpellPreview.transform.position = previewPosition.position;
			activeSpellPreview.transform.parent = previewPosition;
			currentPreviewDuration = activeSpellPreview.duration;
		}
	}
}
