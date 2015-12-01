using UnityEngine;
using System.Collections;

public class FightSetup : MonoBehaviour {
	public Player[,] teamSelection;
	public PlayerManager level;
	private bool isStarted = false;
	private int[] currentPlayer;

	public void SetTeamSize(int teamCount, int teamSize)
	{
		teamSelection = new Player[teamCount, teamSize];
		currentPlayer = new int[teamCount];
	}

	public void SetPlayer(int team, int playerIndex, Player player)
	{
		teamSelection[team, playerIndex] = player;
	}

	public Player NextPlayer(int team)
	{
		if (isStarted && team < teamSelection.GetLength(0) && currentPlayer[team] < teamSelection.GetLength(1))
		{
			return teamSelection[team, currentPlayer[team]++];
		}
		else
		{
			return null;
		}
	}

	public void StartGame()
	{
		isStarted = true;
		Instantiate<PlayerManager>(level);
		Object.FindObjectOfType<FollowCamera>();
		Destroy(gameObject);
	}

	public bool IsStarted
	{
		get
		{
			return isStarted;
		}
	}
}
