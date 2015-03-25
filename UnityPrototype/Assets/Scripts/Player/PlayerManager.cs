using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour, IFixedUpdate {

	public Transform selectionCursor;
	public GUIText countdownTimer;

	public float turnLength = 10.0f;
	public float stableWaitTime = 1.0f;
	public float stoppedTolerance = 0.1f;

	private PlayerHUD playerHUD;

	private TimeManager timeManager;
	private UpdateManager updateManager;

	private float remainingTime;
	private float stableTimer;
	private float gameTime;

	private List<Player> players = new List<Player>();

	private int teamCount = 0;

	private bool[] hasPlayerGone;
	private int currentTurn = -1;

	private bool isSelectingPlayer;
	private int currentSelection;

	private void UpdateHUD()
	{
		playerHUD.CurrentPlayer = CurrentPlayer;
	}
	
	public Player CurrentPlayer
	{
		get
		{
			if (currentSelection != -1)
			{
				return players[currentSelection];
			}
			else
			{
				return null;
			}
		}
	}

	private bool AllPlayersHaveGone
	{
		get
		{
			foreach (bool playerGone in hasPlayerGone)
			{
				if (!playerGone)
				{
					return false;
				}
			}

			return true;
		}
	}

	private void SelectNextPlayer()
	{
		if (!AllPlayersHaveGone)
		{
			do
			{
				currentSelection = (currentSelection + 1) % players.Count;
			} while (hasPlayerGone[currentSelection]);
		}

		UpdateHUD();
	}
	
	private void SelectPrevPlayer()
	{
		if (!AllPlayersHaveGone)
		{
			do
			{
				--currentSelection;

				if (currentSelection < 0)
				{
					currentSelection = players.Count - 1;
				}
			} while (hasPlayerGone[currentSelection]);
		}

		UpdateHUD();
	}

	private void StartSelectedPlayer()
	{
		players[currentSelection].StartRecording();
		hasPlayerGone[currentSelection] = true;
		updateManager.Paused = false;

		isSelectingPlayer = false;
		remainingTime = turnLength;
	}

	private void StartTurn()
	{
		updateManager.Paused = true;
		isSelectingPlayer = true;
		
		if (AllPlayersHaveGone)
		{
			currentTurn = (currentTurn + 1) % teamCount;

			for (int i = 0; i < hasPlayerGone.Length; ++i)
			{
				hasPlayerGone[i] = players[i].team != currentTurn;
			}

			timeManager.TakeSnapshot();
		}
		else
		{
			gameTime -= turnLength;
			timeManager.Rewind();
		}
		
		SelectNextPlayer();
	}

	// Use this for initialization
	void Start () {
		updateManager = GetComponent<UpdateManager>();
		timeManager = GetComponent<TimeManager>();
		playerHUD = GetComponent<PlayerHUD>();

		updateManager.AddPriorityReciever(this);

		hasPlayerGone = new bool[players.Count];
		
		for (int i = 0; i < hasPlayerGone.Length; ++i)
		{
			hasPlayerGone[i] = true;
			teamCount = Mathf.Max(players[i].team + 1, teamCount);
		}

		updateManager.Paused = true;

		StartTurn();
	}

	public void Update()
	{
		if (isSelectingPlayer)
		{
			if (Input.GetButtonDown("Prev"))
			{
				SelectPrevPlayer();
			}
			
			if (Input.GetButtonDown("Next"))
			{
				SelectNextPlayer();
			}
			
			selectionCursor.position = players[currentSelection].transform.position;
			
			if (Input.GetButtonDown("Select"))
			{
				selectionCursor.transform.position = new Vector3(10000.0f, 0.0f, 0.0f);
				StartSelectedPlayer();
			}
		}
	}

	public void FixedUpdateTick(float dt)
	{
		if (!isSelectingPlayer)
		{
			if (remainingTime > 0.0f)
			{
				remainingTime -= dt;
				
				countdownTimer.text = remainingTime.ToString("F");
				
				if (remainingTime <= 0.0f)
				{
					players[currentSelection].BecomeIdle();
				}
				
				gameTime += dt;
			}
			else
			{
				foreach (Player player in players)
				{
					if (player.Velocity.sqrMagnitude > stoppedTolerance * stoppedTolerance)
					{
						stableTimer = stableWaitTime;
						break;
					}
				}
				
				stableTimer -= dt;
				
				if (stableTimer < 0.0f)
				{
					StartTurn();
				}
			}
		}
	}

	public void AddPlayer(Player player)
	{
		players.Add(player);
	}

	public float GameTime
	{
		get
		{
			return gameTime;
		}
	}
}
