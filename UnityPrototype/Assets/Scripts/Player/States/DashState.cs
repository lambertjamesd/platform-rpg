using UnityEngine;
using System.Collections;

public interface IDashStateDelegate
{
	void DashComplete();
	void DashInterrupted();
}

public class DashState : MonoBehaviour, IState {
	
	private Player player;
	private Vector3 velocity;
	private float remainingTime;
	private IDashStateDelegate dashDelegate;
	private bool completedNomrally;

	void Start () {
		if (player == null)
		{
			player = gameObject.GetComponentWithAncestors<Player>();
		}
	}

	void Update() {

	}
	
	public void BeginState(StateMachine stateMachine)
	{
		Vector3 targetPosition = stateMachine.GetParameter<Vector3>("position", player.transform.position);
		float speed = stateMachine.GetParameter<float>("speed", 1.0f);

		Vector3 offset = targetPosition - player.transform.position;
		Vector3 direction = offset.normalized;

		velocity = direction * speed;
		remainingTime = speed == 0.0f ? 0.0f : Vector3.Dot(offset, direction) / speed;

		dashDelegate = stateMachine.GetParameter<IDashStateDelegate>("delegate", null);

		completedNomrally = false;
	}
	
	public void Update(StateMachine stateMachine, float timestep)
	{
		player.Move(velocity * timestep);
		remainingTime -= timestep;

		if (remainingTime <= 0.0f)
		{
			completedNomrally = true;
			
			stateMachine.SetNextState(stateMachine.GetParameter<string>("nextState", "Default"));
			
			if (dashDelegate != null)
			{
				dashDelegate.DashComplete();
			}
		}
	}
	
	public void EndState(StateMachine stateMachine)
	{
		if (!completedNomrally && dashDelegate != null)
		{
			dashDelegate.DashInterrupted();
		}
	}
	
	public object GetCurrentState()
	{
		return new object[]{
			velocity,
			remainingTime,
			dashDelegate,
			completedNomrally
		};
	}
	
	public void RewindToState(object state)
	{
		object[] values = (object[])state;
		
		velocity = (Vector3)values[0];
		remainingTime = (float)values[1];
		dashDelegate = (IDashStateDelegate)values[2];
		completedNomrally = (bool)values[3];
	}
}
