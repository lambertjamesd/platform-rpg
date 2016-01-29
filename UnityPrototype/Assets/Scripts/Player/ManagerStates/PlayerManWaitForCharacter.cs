using UnityEngine;
using System.Collections;

public class PlayerManWaitForCharacter : IState {
	private string nextState;
	private float lastHorizontal;
	private PlayerManager playerManager;
	private CustomFontRenderer fontRenderer;
	
	public PlayerManWaitForCharacter(string nextState, PlayerManager playerManager, CustomFontRenderer fontRenderer)
	{
		this.nextState = nextState;
		this.playerManager = playerManager;
		this.fontRenderer = fontRenderer;
	}
	
	public void BeginState(StateMachine stateMachine)
	{
		
	}
	
	public void Update(StateMachine stateMachine, float timestep)
	{
		if (fontRenderer != null)
		{
			fontRenderer.renderScale = 1.0f;
			fontRenderer.DrawTextScreen(new Vector3(0.75f, 0.25f, 0.5f), playerManager.RemainingTime.ToString("0.0"), CustomFontRenderer.CenterAlign);
		}

		if (!playerManager.IsRunning)
		{
			stateMachine.SetNextState(nextState);
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
