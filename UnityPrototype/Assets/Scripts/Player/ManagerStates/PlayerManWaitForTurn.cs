using UnityEngine;
using System.Collections;

public class PlayerManWaitForTurn : MonoBehaviour, IState {
	public PlayerManager playerManager;
	public string nextState;
	private int startTurn;

	public void BeginState(StateMachine stateMachine)
	{
		startTurn = playerManager.CurrentTurn;
	}

	public void Update()
	{
		
	}

	public void Update(StateMachine stateMachine, float timestep)
	{
		if (startTurn != playerManager.CurrentTurn)
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
