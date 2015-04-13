using UnityEngine;
using System.Collections;

public class MoveState : MonoBehaviour, IState {

	private Player player;

	void Start()
	{
		if (player == null)
		{
			player = gameObject.GetComponentWithAncestors<Player>();
		}
	}

	void Update()
	{

	}
	
	public void BeginState(StateMachine stateMachine)
	{

	}

	public void Update(StateMachine stateMachine, float timestep)
	{
		float horizontalMovement = player.InputSource.State.HorizontalControl;

		Vector3 targetVelocity = horizontalMovement * player.FloorTangent * player.Stats.GetNumberStat("maxMoveSpeed");

		Vector3 velocityDirection = targetVelocity - player.Velocity;

		float accelerationAmount = player.Settings.moveAcceleration * timestep;

		if (velocityDirection.sqrMagnitude <= accelerationAmount * accelerationAmount)
		{
			player.Velocity = targetVelocity;
		}
		else
		{
			player.Velocity += velocityDirection.normalized * accelerationAmount;
		}
		
		player.DefaultMovement(timestep);
		player.ApplyGravity(timestep);
		player.HandleKnockback();


		if (!player.IsGrounded)
		{
			stateMachine.SetNextState("FreeFall");
		}
		else if (player.InputSource.State.JumpButtonDown)
		{
			player.JumpNormal = player.FloorNormal;
			stateMachine.SetNextState("Jump");
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
