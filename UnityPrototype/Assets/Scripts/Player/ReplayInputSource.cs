using UnityEngine;
using System.Collections;

public class ReplayInputSource : IInputSource {

	private InputState currentState;
	private InputRecording source;
	private int inputIndex = 0;
	
	private Transform positionCheckTransform;

	public ReplayInputSource(InputRecording source, Transform playerTransform)
	{
		this.source = source;
		this.positionCheckTransform = playerTransform;
	}
	
	public void FrameStart(InputState previousState)
	{
		if (inputIndex < source.Length)
		{
			currentState = source.GetState(inputIndex);

			// check to see if the position is out of sync
			if (positionCheckTransform != null && currentState.PositionCheck != positionCheckTransform.localPosition)
			{
				currentState = new InputState(previousState);
				inputIndex = source.Length;
			}

			++inputIndex;
		}
		else
		{
			currentState = new InputState(previousState);
		}
	}
	
	
	public InputState State
	{
		get
		{
			return currentState;
		}
	}
}
