using UnityEngine;
using System.Collections;

public class ControllerInputSource : IInputSource
{
	private InputState previousState = null;
	private InputState currentState = null;
	
	public ControllerInputSource()
	{

	}
	
	public void FrameStart()
	{
		previousState = currentState;
		Vector3 aimDirection = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0.0f);
		bool[] fireButtons = new bool[]{Input.GetButton("Fire0"), Input.GetButton("Fire1"), Input.GetButton("Fire2")};
		currentState = new InputState(previousState, Input.GetAxis("Horizontal"), Input.GetButton("Jump"), fireButtons, aimDirection);
	}
	
	public InputState State
	{
		get
		{
			return currentState;
		}
	}
}
