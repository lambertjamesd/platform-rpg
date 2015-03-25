using UnityEngine;
using System.Collections;

public class FreeFallState : MonoBehaviour, IState {
	
	private Player player;
	
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
		
	}
	
	public void Update(StateMachine stateMachine, float timestep)
	{
		float maxMoveSpeed = player.Stats.GetNumberStat("maxMoveSpeed");
		float horizontalMovement = player.InputSource.State.HorizontalControl;
		float targetRightSpeed = Mathf.Clamp(player.Velocity.x + horizontalMovement * maxMoveSpeed, -maxMoveSpeed, maxMoveSpeed);

		player.ApplyGravity(timestep);
		player.HandleKnockback();
		player.Velocity += Vector3.right * (targetRightSpeed - player.Velocity.x) * player.settings.airAcceleration * timestep;
		player.Move(player.Velocity * timestep);

		if (player.IsGrounded)
		{
			stateMachine.SetNextState("GroundMove");
		}
		else if (player.IsWallSliding)
		{
			stateMachine.SetNextState("WallSlide");
		}
	}
	
	public void EndState(StateMachine stateMachine)
	{
		
	}
}
