using UnityEngine;
using System.Collections;

public interface IRootedStateDelegate
{
	bool StillRooted { get; }
}

public class RootedState : MonoBehaviour, IState {
	
	private Player player;
	private IRootedStateDelegate rootedDelegate;
	
	void Start()
	{
		if (player == null)
		{
			player = gameObject.GetComponentWithAncestors<Player>();
		}
	}
	
	void Update() {
		
	}
	
	public void BeginState(StateMachine stateMachine)
	{
		player.Velocity = Vector3.zero;
		rootedDelegate = stateMachine.GetParameter<IRootedStateDelegate>("delegate", null);
		player.Status.isRooted = true;
	}
	
	public void Update(StateMachine stateMachine, float timestep)
	{
		if (rootedDelegate == null || !rootedDelegate.StillRooted)
		{
			stateMachine.SetNextState(stateMachine.GetParameter<string>("nextState", "Default"));
		}
	}
	
	public void EndState(StateMachine stateMachine)
	{
		player.Status.isRooted = false;
	}
	
	public object GetCurrentState()
	{
		return rootedDelegate;
	}
	
	public void RewindToState(object state)
	{
		rootedDelegate = (IRootedStateDelegate)state;
	}
}
