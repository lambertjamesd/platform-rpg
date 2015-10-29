using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum PlayerManagerMode {
	Fight,
	Preview
};

public class PlayerManager : MonoBehaviour, IFixedUpdate {

	public Transform selectionCursor;
	public CustomFontRenderer fontRenderer;

	public float turnLength = 10.0f;
	public StateMachine stateMachine;
	public PlayerManTextState textState;
	public PlayerManWaitForTurn waitTurnState;

	public PlayerManagerMode mode = PlayerManagerMode.Fight;

	private PlayerHUD playerHUD;

	private TimeManager timeManager;
	private UpdateManager updateManager;

	private float remainingTime;

	private List<Player> players = new List<Player>();

	private int teamCount = 0;

	private bool[] hasPlayerGone;
	private int currentTurn = -1;
	private int numberPlayersStarted = 0;


	public float freeCameraSpeed = 20.0f;
	private Transform freeCameraFollow;
	private bool isFreeCamera;
	private bool isSelectingPlayer;
	private int currentSelection;
	private float lastHorizontal;

	private FollowCamera cameraAI;

	private bool lastFrame = false;

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

	public int CurrentTurn
	{
		get
		{
			return currentTurn;
		}
	}

	public IEnumerable<Player> PlayersOnLayers(int bitmask)
	{
		return players.Where( player => player.gameObject.activeSelf && (CollisionLayers.AllyLayers(player.gameObject.layer) & bitmask) != 0 );
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
		players[currentSelection].StartTurn(numberPlayersStarted);
		players[currentSelection].StartRecording();
		hasPlayerGone[currentSelection] = true;
		updateManager.Paused = false;

		isSelectingPlayer = false;
		remainingTime = turnLength;
		playerHUD.showSpellDescriptions = false;
	}

	private void ChangeTeams()
	{	
		for (int i = 0; i < players.Count; ++i)
		{
			if (players[i].Team == currentTurn)
			{
				players[i].EndTurn();
			}
		}

		currentTurn = (currentTurn + 1) % teamCount;
		
		for (int i = 0; i < hasPlayerGone.Length; ++i)
		{
			hasPlayerGone[i] = !players[i].gameObject.activeSelf || players[i].Team != currentTurn;
		}
		
		timeManager.TakeSnapshot();
	}

	private void StartTurn()
	{
		updateManager.Paused = true;
		isSelectingPlayer = true;
		
		if (AllPlayersHaveGone)
		{
			ChangeTeams();
			playerHUD.showSpellDescriptions = true;
		}
		else
		{
			++numberPlayersStarted;
			timeManager.Rewind();
		}
		
		SelectNextPlayer();
	}

	// Use this for initialization
	void Start () {
		updateManager = GetComponent<UpdateManager>();
		timeManager = GetComponent<TimeManager>();
		
		updateManager.AddLateReciever(this);

		if (mode == PlayerManagerMode.Fight)
		{
			playerHUD = GetComponent<PlayerHUD>();
			cameraAI = Camera.main.GetComponent<FollowCamera>();

			hasPlayerGone = new bool[players.Count];
			
			for (int i = 0; i < hasPlayerGone.Length; ++i)
			{
				hasPlayerGone[i] = true;
				teamCount = Mathf.Max(players[i].Team + 1, teamCount);
			}

			updateManager.Paused = true;

			StartTurn();

			stateMachine = new StateMachine((string stateName) => {
				switch (stateName)
				{
				case "Start":
					textState.text = "TEAM " + (currentTurn + 1) + " START";
					textState.color = TeamColors.GetColor(currentTurn);
					textState.nextState = "WaitForTurn";
					return textState;
				case "WaitForTurn":
					waitTurnState.nextState = "Start";
					return waitTurnState;
				}
				return null;
			});
		}
		else if (mode == PlayerManagerMode.Preview)
		{

		}
	}

	public void Update()
	{
		if (mode == PlayerManagerMode.Fight)
		{
			if (isFreeCamera)
			{
				if (Input.GetButtonDown("FreeCamera"))
				{
					isFreeCamera = false;
					cameraAI.FollowTarget = players[currentSelection].transform;
				}
				else
				{
					freeCameraFollow.transform.position += freeCameraSpeed * Time.deltaTime * (
						cameraAI.transform.up * Input.GetAxis("Vertical") +
						cameraAI.transform.right * Input.GetAxis("Horizontal")
						);
					
					fontRenderer.renderScale = 0.5f;
					fontRenderer.DrawTextScreen(new Vector3(0.5f, 0.25f, 0.5f), "PRESS LEFT CTRL TO RETURN", CustomFontRenderer.CenterAlign);
				}
			}
			else if (isSelectingPlayer)
			{
				cameraAI.FollowTarget = CurrentPlayer.transform;

				if (Input.GetButtonDown("FreeCamera"))
				{
					isFreeCamera = true;

					if (freeCameraFollow == null)
					{
						GameObject target = new GameObject();
						freeCameraFollow = target.transform;
					}

					freeCameraFollow.transform.position = players[currentSelection].transform.position;
					cameraAI.FollowTarget = freeCameraFollow.transform;
				}
				else
				{
					float horizontal = Input.GetAxis("Horizontal");

					if (Input.GetButtonDown("Prev") || lastHorizontal > -0.5f && horizontal <= -0.5f)
					{
						SelectPrevPlayer();
					}
					
					if (Input.GetButtonDown("Next") || lastHorizontal < 0.5f && horizontal >= 0.5f)
					{
						SelectNextPlayer();
					}
					
					selectionCursor.position = players[currentSelection].transform.position;
					
					if (Input.GetButtonDown("Select"))
					{
						selectionCursor.transform.position = new Vector3(10000.0f, 0.0f, 0.0f);
						StartSelectedPlayer();
					}

					fontRenderer.renderScale = 0.5f;
					fontRenderer.DrawTextScreen(new Vector3(0.5f, 0.25f, 0.5f), "PRESS LEFT CTRL FOR FREE CAMERA", CustomFontRenderer.CenterAlign);

					lastHorizontal = horizontal;
				}
			}
			else
			{
				fontRenderer.renderScale = 1.0f;
				fontRenderer.DrawTextScreen(new Vector3(0.75f, 0.25f, 0.5f), remainingTime.ToString("0.0"), CustomFontRenderer.CenterAlign);
			}

			stateMachine.Update(Time.deltaTime);
		}
	}

	public void FixedUpdateTick(float dt)
	{
		if (!isSelectingPlayer && mode == PlayerManagerMode.Fight)
		{
			if (remainingTime > 0.0f)
			{
				remainingTime -= dt;
				
				if (remainingTime <= 0.0f)
				{
					players[currentSelection].LastFrame();
				}

				lastFrame = true;
			}
			else
			{
				if (lastFrame)
				{
					players[currentSelection].BecomeIdle();
					lastFrame = false;
				}
				else
				{
					StartTurn();
				}
			}
		}
	}

	public void AddPlayer(Player player)
	{
		player.Players = this;
		players.Add(player);
	}

	public float RemainingTime
	{
		get
		{
			return remainingTime;
		}
	}
}
