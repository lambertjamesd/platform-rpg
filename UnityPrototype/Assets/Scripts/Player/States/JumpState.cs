﻿using UnityEngine;
using System.Collections;

public class JumpState : MonoBehaviour, IState {
	
	private Player player;
	private float jumpControlTime = 0.0f;

	private float initialJumpImpulse;
	private float jumpAcceration;
	private float jumpHeightControlWindow;
	
	void Start()
	{
		if (player == null)
		{
			player = gameObject.GetComponentWithAncestors<Player>();
		}
	}
	
	void Update () {
		
	}

	private void CalcJumpValues()
	{
		float maxJumpHeight = player.Stats.GetNumberStat("maxJumpHeight");
		float minJumpHeight = Mathf.Min(player.Stats.GetNumberStat("minJumpHeight"), maxJumpHeight);

		jumpHeightControlWindow = player.Stats.GetNumberStat("jumpHeightControlWindow");
		initialJumpImpulse = Mathf.Sqrt(2.0f * minJumpHeight * -Physics.gravity.y);
		
		float g = Physics.gravity.y;
		float c = jumpHeightControlWindow;
		float v0 = initialJumpImpulse;
		
		float test = g * g * c * c + 4.0f * g * (c * v0 - 2.0f * maxJumpHeight);
		
		if (test < 0.0f)
		{
			Debug.LogError("Invalid jump configuration");
		}
		else
		{
			float v1 = (g * c + Mathf.Sqrt(test)) / 2.0f;
			jumpAcceration = (v1 - v0) / jumpHeightControlWindow - g;
		}
	}
	
	public void BeginState(StateMachine stateMachine)
	{
		CalcJumpValues();

		player.Velocity += player.JumpNormal * initialJumpImpulse;
		jumpControlTime = jumpHeightControlWindow;

		if (player.VisualAnimator != null)
		{
			player.VisualAnimator.SetTrigger("Jump");
		}
	}
	
	public void Update(StateMachine stateMachine, float timestep)
	{
		float horizontalMovement = player.InputSource.State.HorizontalControl;

		player.ApplyGravity(timestep);
		player.HandleKnockback();
		player.Velocity += Vector3.up * jumpAcceration * timestep 
			+ horizontalMovement * Vector3.right * player.settings.airAcceleration * timestep;
		player.Move(player.Velocity * timestep);

		if (jumpControlTime <= 0.0 || !player.InputSource.State.JumpButton)
		{
			stateMachine.SetNextState("FreeFall");
		}

		jumpControlTime -= timestep;
	}
	
	public void EndState(StateMachine stateMachine)
	{
		
	}
}