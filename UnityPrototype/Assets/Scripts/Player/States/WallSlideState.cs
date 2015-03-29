using UnityEngine;
using System.Collections;

public class WallSlideState : MonoBehaviour, IState {
	
	private Player player;
	private float stickTimer = 0.0f;
	
	void Start()
	{
		if (player == null)
		{
			player = gameObject.GetComponentWithAncestors<Player>();
		}
	}
	
	void Update () {
		
	}
	
	public void BeginState(StateMachine stateMachine)
	{
		stickTimer = player.Settings.wallStickTime;
	}
	
	public void Update(StateMachine stateMachine, float timestep)
	{
		float horizontalMovement = player.InputSource.State.HorizontalControl;
		
		player.ApplyGravity(timestep);
		player.HandleKnockback();
		player.Velocity *= Mathf.Pow(player.Settings.wallSlideDamping, timestep);
		player.Velocity -= player.WallNormal * player.settings.airAcceleration * timestep;
		player.Move(player.Velocity * timestep);

		if (player.WallNormal.x * horizontalMovement >= 0.0f)
		{
			stickTimer -= timestep;
		}
		else
		{
			stickTimer = player.Settings.wallStickTime;
		}

		if (player.InputSource.State.JumpButtonDown)
		{
			player.JumpNormal = player.WallNormal + Vector3.up;
			stateMachine.SetNextState("Jump");
		}
		else if (stickTimer <= 0.0f || !player.IsWallSliding)
		{
			stateMachine.SetNextState("FreeFall");
		}
		else if (player.IsGrounded)
		{
			stateMachine.SetNextState("GroundMove");
		}
	}
	
	public void EndState(StateMachine stateMachine)
	{
		
	}
	
	public object GetCurrentState()
	{
		return new object[]{
			stickTimer
		};
	}
	
	public void RewindToState(object state)
	{
		object[] values = (object[])state;
		
		stickTimer = (float)values[0];
	}
}
