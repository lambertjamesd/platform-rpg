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
		player.DefaultMovement(timestep);
		player.ApplyGravity(timestep);
		player.HandleKnockback();
		HandleHorizontalControl(player, timestep);

		if (player.IsGrounded)
		{
			stateMachine.SetNextState("GroundMove");
		}
		else if (player.IsWallSliding)
		{
			stateMachine.SetNextState("WallSlide");
		}
	}

	public static void HandleHorizontalControl(Player player, float timestep)
	{
		float maxMoveSpeed = player.Stats.GetNumberStat("maxMoveSpeed");
		float horizontalMovement = player.InputSource.State.HorizontalControl;
		float targetRightSpeed = Mathf.Clamp(player.Velocity.x + horizontalMovement * maxMoveSpeed, -maxMoveSpeed, maxMoveSpeed);
		player.Velocity += Vector3.right * Mathf.Sign(targetRightSpeed - player.Velocity.x) * player.Stats.GetNumberStat("airAcceleration") * timestep;
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
