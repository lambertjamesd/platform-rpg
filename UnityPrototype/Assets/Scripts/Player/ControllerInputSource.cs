using UnityEngine;
using System.Collections;

public class ControllerInputSource : IInputSource
{
	private InputState currentState = null;

	private static readonly float DEAD_ZONE = 0.1f;
	
	public ControllerInputSource()
	{

	}
	
	public void FrameStart(InputState previousState)
	{
		Vector3 aimDirection = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0.0f);

		float aimMagnitude = aimDirection.magnitude;

		if (aimMagnitude > 0.0f)
		{
			float scaleFactor = Mathf.Max(aimMagnitude - DEAD_ZONE, 0.0f) / (1.0f - DEAD_ZONE);
			aimDirection *= (scaleFactor / aimMagnitude);
		}

		bool[] fireButtons = new bool[]{Input.GetButton("Fire0"), Input.GetButton("Fire1"), Input.GetButton("Fire2")};
		currentState = new InputState(previousState, aimDirection.x, Input.GetButton("Jump"), fireButtons, aimDirection);
	}
	
	public InputState State
	{
		get
		{
			return currentState;
		}
	}
}
