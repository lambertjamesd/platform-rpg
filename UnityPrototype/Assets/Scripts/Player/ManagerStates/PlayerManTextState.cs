using UnityEngine;
using System.Collections;

public class PlayerManTextState : MonoBehaviour, IState {
	public PlayerManager playerManager;
	public string text;
	public Color color;
	public float duration;
	public string nextState;

	public AnimationCurve transparencyOverTime = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);
	public AnimationCurve yOffsetOverTime = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 0.0f);

	private float lifetime;

	public void BeginState(StateMachine stateMachine)
	{
		lifetime = duration;
	}

	public void Update()
	{

	}
	
	public void Update(StateMachine stateMachine, float timestep)
	{
		if (lifetime > 0.0f)
		{
			float lerp = lifetime / duration;

			playerManager.fontRenderer.renderScale = 1.0f;
			playerManager.fontRenderer.DrawTextScreen(new Vector3(0.5f, 0.5f, 0.5f), text, CustomFontRenderer.CenterAlign, index => {
				Color colorPass = color;
				colorPass.a = transparencyOverTime.Evaluate(lerp);
				return new CustomFontRenderer.GlyphVariation(Vector3.up * yOffsetOverTime.Evaluate(lerp), colorPass, 1.0f);
			});

			lifetime -= timestep;

			if (lifetime <= 0.0f)
			{
				stateMachine.SetNextState(nextState);
			}
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
