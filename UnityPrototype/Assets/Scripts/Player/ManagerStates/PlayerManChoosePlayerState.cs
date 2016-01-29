using UnityEngine;
using System.Collections;

public class PlayerManChoosePlayerState : IState {
	private string nextState;
	private FollowCamera cameraAI;
	private float lastHorizontal;
	private PlayerManager playerManager;
	private CustomFontRenderer fontRenderer;
	
	public PlayerManChoosePlayerState(string nextState, FollowCamera cameraAI, PlayerManager playerManager, CustomFontRenderer fontRenderer)
	{
		this.nextState = nextState;
		this.cameraAI = cameraAI;
		this.playerManager = playerManager;
		this.fontRenderer = fontRenderer;
	}
	
	public void BeginState(StateMachine stateMachine)
	{
		playerManager.StartTurn();
	}

	public void Update(StateMachine stateMachine, float timestep)
	{
		cameraAI.FollowTarget = playerManager.CurrentPlayer.transform;
		
		if (Input.GetButtonDown("FreeCamera"))
		{
			stateMachine.SetNextState("FreeCamera");
		}
		else
		{
			float horizontal = Input.GetAxis("Horizontal");
			
			if (Input.GetButtonDown("Prev") || lastHorizontal > -0.5f && horizontal <= -0.5f)
			{
				playerManager.SelectPrevPlayer();
			}
			
			if (Input.GetButtonDown("Next") || lastHorizontal < 0.5f && horizontal >= 0.5f)
			{
				playerManager.SelectNextPlayer();
			}
			
			/*if (selectionCursor != null)
			{
				selectionCursor.position = players[currentSelection].transform.position;
			}*/

			if (Input.GetButtonDown("Select"))
			{
				/*if (selectionCursor != null)
				{
					selectionCursor.transform.position = new Vector3(10000.0f, 0.0f, 0.0f);
				}*/
				
				playerManager.StartSelectedPlayer();
				stateMachine.SetNextState(nextState);
			}
			
			if (fontRenderer != null)
			{
				fontRenderer.renderScale = 0.5f;
				fontRenderer.DrawTextScreen(new Vector3(0.5f, 0.25f, 0.5f), "PRESS LEFT CTRL FOR FREE CAMERA", CustomFontRenderer.CenterAlign);
			}
			
			lastHorizontal = horizontal;
		}
	}
	
	public void EndState(StateMachine stateMachine)
	{
	}

	
	public object GetCurrentState()
	{
		return null;
	}
	
	public void RewindToState(object state)
	{
		
	}
}
