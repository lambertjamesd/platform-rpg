using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum PlayerManagerMode {
	Fight,
	Preview,
	RecordPreview
};

public class PlayerManager : MonoBehaviour, IFixedUpdate {

	public Transform selectionCursor;
	public CustomFontRenderer fontRenderer;

	public float turnLength = 10.0f;
	public StateMachine stateMachine;
	public PlayerManTextState textState;
	public PlayerManWaitForTurn waitTurnState;

	public PlayerManagerMode mode = PlayerManagerMode.Fight;
	public TextAsset recording;

	private PlayerHUD playerHUD;

	private TimeManager timeManager;
	private UpdateManager updateManager;

	private float remainingTime;

	private List<Player> players = new List<Player>();

	private int teamCount = 0;

	private bool[] hasPlayerGone;
	private int currentTurn = -1;
	private int numberPlayersStarted = 0;

	private int currentSelection;
	private float lastHorizontal;

	private FollowCamera cameraAI;

	private bool lastFrame = false;

	private void UpdateHUD()
	{
		if (playerHUD != null)
		{
			playerHUD.CurrentPlayer = CurrentPlayer;
		}
	}

	public bool IsRunning
	{
		get
		{
			return !updateManager.Paused;
		}
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

	public void SelectNextPlayer()
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
	
	public void SelectPrevPlayer()
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

	public void StartSelectedPlayer()
	{
		players[currentSelection].StartTurn(numberPlayersStarted);
		players[currentSelection].StartRecording();
		DeterminismDebug.GetSingleton().StartRecording(players[currentSelection].gameObject);
		hasPlayerGone[currentSelection] = true;
		updateManager.Paused = false;

		remainingTime = turnLength;

		if (playerHUD != null)
		{
			playerHUD.showSpellDescriptions = false;
		}
	}

	public bool IsPaused
	{
		get
		{
			return updateManager.Paused;
		}
	}

	private static int ComparePlayers(Player a, Player b)
	{
		if (a.Team == b.Team)
		{
			return a.PlayerIndex.CompareTo(b.PlayerIndex);
		}
		else
		{
			return a.Team.CompareTo(b.Team);
		}
	}

	private void SaveRecording()
	{
		SimpleJSON.JSONArray result = new SimpleJSON.JSONArray();

		List<Player> sortedPlayers = new List<Player>(players);

		sortedPlayers.Sort(ComparePlayers);

		foreach (Player player in sortedPlayers) {
			if (player.LastRecording == null)
			{
				result.Add(new SimpleJSON.JSONArray());
			}
			else
			{
				result.Add(player.LastRecording.Serialize());
			}
		}

#if UNITY_EDITOR
		string path = (recording == null) ? "input-recording.json" : AssetDatabase.GetAssetPath(recording) + ".new.json";
#else
		string path = "input-recording.json";
#endif
		StreamWriter file = File.CreateText(path);
		file.Write(result.ToString());
		file.Close();

		Debug.Log("Input recording saved to " + path);
	}

	private void ChangeTeams()
	{	
		for (int i = 0; i < players.Count; ++i)
		{
			if (players[i].Team == currentTurn || mode != PlayerManagerMode.Fight)
			{
				players[i].EndTurn();
			}
		}

		if (mode == PlayerManagerMode.RecordPreview)
		{
			SaveRecording();
		}

		currentTurn = (currentTurn + 1) % teamCount;
		
		for (int i = 0; i < hasPlayerGone.Length; ++i)
		{
			hasPlayerGone[i] = !players[i].gameObject.activeSelf || (players[i].Team != currentTurn && mode == PlayerManagerMode.Fight);
		}
		
		timeManager.TakeSnapshot();
		timeManager.CleanUpSnapshots(1, timeManager.SnapshotCount - 1);

		DeterminismDebug.GetSingleton().Reset();
	}

	private void PauseGameplay()
	{
		updateManager.Paused = true;
	}

	public void StartTurn()
	{
		if (AllPlayersHaveGone)
		{
			ChangeTeams();

			if (playerHUD != null)
			{
				playerHUD.showSpellDescriptions = true;
			}
		}
		else
		{
			++numberPlayersStarted;
			timeManager.Rewind();
			DeterminismDebug.GetSingleton().Rewind();
		}
		
		SelectNextPlayer();
	}

	// Use this for initialization
	void Start () {
		updateManager = GetComponent<UpdateManager>();
		timeManager = GetComponent<TimeManager>();
		
		updateManager.AddLateReciever(this);

		if (mode == PlayerManagerMode.Fight || mode == PlayerManagerMode.RecordPreview)
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
			PauseGameplay();

			stateMachine = new StateMachine((string stateName) => {
				switch (stateName)
				{
				case "Start":
					if (textState == null)
					{
						return new PlayerManChoosePlayerState("WaitForCharacter", cameraAI, this, fontRenderer);
					}
					else
					{
						textState.text = "TEAM " + (currentTurn + 1) + " START";
						textState.color = TeamColors.GetColor(currentTurn);
						textState.nextState = "ChoosePlayer";
						return textState;
					}
				case "WaitForTurn":
					waitTurnState.nextState = "Start";
					return waitTurnState;
				case "ChoosePlayer":
					return new PlayerManChoosePlayerState("WaitForCharacter", cameraAI, this, fontRenderer);
				case "WaitForCharacter":
					return new PlayerManWaitForCharacter("ChoosePlayer", this, fontRenderer);
				case "FreeCamera":
					return new PlayerManFreeCamera("ChoosePlayer", cameraAI, fontRenderer);
				}
				return null;
			});
		}
		else if (mode == PlayerManagerMode.Preview)
		{
			if (recording != null)
			{
				SimpleJSON.JSONArray recordingData = SimpleJSON.JSON.Parse(recording.text).AsArray;

				List<Player> sortedPlayers = new List<Player>(players);

				sortedPlayers.Sort(ComparePlayers);

				for (int i = 0; i < recordingData.Count && i < sortedPlayers.Count; ++i)
				{
					sortedPlayers[i].Playback(InputRecording.Deserialize(recordingData[i]));
				}
			}

			updateManager.Paused = false;
		}
	}

	public void Update()
	{
		if (stateMachine != null)
		{
			stateMachine.Update(Time.deltaTime);
		}
	}

	public void FixedUpdateTick(float dt)
	{
		if (IsRunning && (mode == PlayerManagerMode.Fight || mode == PlayerManagerMode.RecordPreview))
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
					PauseGameplay();
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
