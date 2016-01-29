using UnityEngine;
using System.Collections;

public class PlayerManFreeCamera : IState {
	private string nextState;
	private FollowCamera cameraAI;
	private CustomFontRenderer fontRenderer;
	private Transform startingTarget;
	private GameObject emptyTarget;
	
	private float freeCameraSpeed = 20.0f;

	public PlayerManFreeCamera(string nextState, FollowCamera cameraAI, CustomFontRenderer fontRenderer)
	{
		this.nextState = nextState;
		this.cameraAI = cameraAI;
		this.fontRenderer = fontRenderer;
	}

	public void BeginState(StateMachine stateMachine)
	{
		startingTarget = cameraAI.FollowTarget;

		emptyTarget = new GameObject();
		emptyTarget.transform.position = startingTarget.position;

		cameraAI.FollowTarget = emptyTarget.transform;
	}
	
	public void Update(StateMachine stateMachine, float timestep)
	{
		if (Input.GetButtonDown("FreeCamera"))
		{
			stateMachine.SetNextState(nextState);
		}
		else
		{
			emptyTarget.transform.position += freeCameraSpeed * Time.deltaTime * (
				cameraAI.transform.up * Input.GetAxis("Vertical") +
				cameraAI.transform.right * Input.GetAxis("Horizontal")
				);
			
			if (fontRenderer != null)
			{
				fontRenderer.renderScale = 0.5f;
				fontRenderer.DrawTextScreen(new Vector3(0.5f, 0.25f, 0.5f), "PRESS LEFT CTRL TO RETURN", CustomFontRenderer.CenterAlign);
			}
		}
	}
	
	public void EndState(StateMachine stateMachine)
	{
		cameraAI.FollowTarget = startingTarget;
		GameObject.Destroy(emptyTarget);
	}
	
	public object GetCurrentState()
	{
		return null;
	}
	
	public void RewindToState(object state)
	{
		
	}
}
