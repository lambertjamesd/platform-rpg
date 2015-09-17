using UnityEngine;
using System.Collections;

public class ReplayInputSource : IInputSource {

	private InputState currentState;
	private InputRecording source;
	private int inputIndex = 0;

	public ReplayInputSource(InputRecording source)
	{
		this.source = source;
	}
	
	public void FrameStart(InputState previousState)
	{
		if (inputIndex < source.Length)
		{
			currentState = source.GetState(inputIndex);
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
