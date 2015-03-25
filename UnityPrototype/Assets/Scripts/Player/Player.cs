﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerState
{
	private Vector3 position;
	private Vector3 velocity;
	private float health;
	private InputRecording input;

	public PlayerState(Vector3 position, Vector3 velocity, float health, InputRecording input)
	{
		this.position = position;
		this.velocity = velocity;
		this.health = health;
		this.input = input;
	}

	public Vector3 Position
	{
		get
		{
			return position;
		}
	}

	public Vector3 Velocity
	{
		get
		{
			return velocity;
		}
	}

	public float Health
	{
		get
		{
			return health;
		}
	}

	public InputRecording InputRecord
	{
		get
		{
			return input;
		}

		set
		{
			input = value;
		}
	}
}

public class Player : MonoBehaviour, IFixedUpdate, ITimeTravelable, ITeleportable, IDashable {

	public MoveState moveState;
	public JumpState jumpState;
	public FreeFallState freefallState;
	public WallSlideState wallSlideState;
	public DashState dashState;

	public int team;

	public PlayerSettings settings;
	private PlayerStats stats;

	public KnockbackHandler knockback;

	public GameObject visual;
	private Animator visualAnimator;

	private TilemapOverlapCorrecter overlapCorrecter;
	private UpdateManager updateManager;
	private TimeManager timeManager;
	private PlayerManager playerManager;

	private IInputSource inputSource;

	private StateMachine stateMachine;
	private CharacterController characterController;
	private SpellCaster spellCaster;
	private Damageable damageable;
	private Vector3 velocity;
	private Vector3 floorNormal = Vector3.up;
	private float floorUpTolerance;

	private bool isGrounded;

	private Vector3 wallNormal;
	private bool isWallSliding;

	private PlayerState lastState;

	public static readonly int SPELL_COUNT = 3;

	public SpellCaster Caster
	{
		get
		{
			return spellCaster;
		}
	}

	public IInputSource InputSource
	{
		get
		{
			return inputSource;
		}
	}

	public Vector3 JumpNormal
	{
		get; set;
	}

	public Vector3 WallNormal
	{
		get
		{
			return wallNormal;
		}
	}

	public bool IsWallSliding
	{
		get
		{
			return isWallSliding;
		}
	}

	public Vector3 FloorNormal
	{
		get
		{
			return floorNormal;
		}
	}

	public bool IsGrounded
	{
		get
		{
			return isGrounded;
		}
	}

	public Vector3 FloorTangent
	{
		get
		{
			return new Vector3(floorNormal.y, -floorNormal.x, 0.0f);
		}
	}

	public Vector3 Velocity
	{
		get
		{
			return velocity;
		}

		set
		{
			velocity = value;
		}
	}

	public void LimitVelocity(float speed)
	{
		if (Velocity.sqrMagnitude > speed * speed)
		{
			Velocity = Velocity.normalized * speed;
		}
	}

	public void ApplyGravity(float timestep)
	{
		Velocity += Physics.gravity * timestep;
	}

	public Vector3 KnockbackImpulse
	{
		get
		{
			return knockback.CurrentImpulse;
		}
	}

	public void ClearKnockback()
	{
		knockback.ClearImpulse();
	}

	public void HandleKnockback()
	{
		Velocity += KnockbackImpulse;
		ClearKnockback();
	}

	public void Move(Vector3 amount)
	{
		amount.z = 0.0f;
				
		isGrounded = false;
		isWallSliding = false;
		characterController.Move(amount);
	}

	public PlayerSettings Settings
	{
		get
		{
			return settings;
		}
	}

	public PlayerStats Stats
	{
		get
		{
			return stats;
		}
	}

	public Animator VisualAnimator
	{
		get
		{
			return visualAnimator;
		}
	}

	private void EnsureInitialized()
	{
		if (characterController == null)
		{
			characterController = GetComponent<CharacterController>();
			spellCaster = GetComponent<SpellCaster>();
			damageable = GetComponent<Damageable>();
			floorUpTolerance = Mathf.Cos(characterController.slopeLimit * Mathf.Deg2Rad);
			overlapCorrecter = gameObject.GetComponentWithAncestors<TilemapOverlapCorrecter>();
			stats = gameObject.GetComponent<PlayerStats>();

			if (visual != null)
			{
				visualAnimator = visual.GetComponent<Animator>();
			}
			
			inputSource = new IdleInputSource();
			
			settings.RecalculateDerivedValues();
			
			stateMachine = new StateMachine((string stateName) => {
				switch (stateName)
				{
				case "Start":
				case "GroundMove":
					return moveState;
				case "Jump":
					return jumpState;
				case "Default":
				case "FreeFall":
					return freefallState;
				case "WallSlide":
					return wallSlideState;
				case "Dash":
					return dashState;
				}
				
				return null;
			});
		}
	}

	public void StartRecording()
	{
		EnsureInitialized();

		lastState.InputRecord = new InputRecording();
		inputSource = new RecordInputSource(lastState.InputRecord, new ControllerInputSource());
	}

	public void BecomeIdle()
	{
		inputSource = new IdleInputSource();
	}

	void Awake() {
		EnsureInitialized();
	}

	public int GetSpellCount()
	{
		return spellCaster.GetSpellCount();
	}

	public SpellDescription GetSpell(int index)
	{
		return spellCaster.GetSpell(index);
	}

	public float GetSpellCooldown(int index)
	{
		return Mathf.Max(0.0f, spellCaster.GetSpellCooldown(index) - playerManager.GameTime);
	}

	public void OnEnable()
	{	
		updateManager = updateManager ?? gameObject.GetComponentWithAncestors<UpdateManager>();
		timeManager = timeManager ?? gameObject.GetComponentWithAncestors<TimeManager>();
		playerManager = gameObject.GetComponentWithAncestors<PlayerManager>();

		this.AddToUpdateManager(updateManager);
		timeManager.AddTimeTraveler(this);
	}

	public void OnDisable()
	{
		this.RemoveFromUpdateManager(updateManager);
	}

	public void FixedUpdateTick(float timestep)
	{
		inputSource.FrameStart();
		stateMachine.Update(timestep);

		if (visualAnimator != null)
		{
			Vector3 visualScale = visual.transform.localScale;

			visualAnimator.SetBool("Grounded", IsGrounded);
			visualAnimator.SetFloat("Speed", Mathf.Abs(velocity.x) * 4.0f);
			visualAnimator.SetFloat("YVelocity", velocity.y / Mathf.Abs(visualScale.y));

			if (velocity.x > 0.0f)
			{
				visualScale.x = Mathf.Abs(visualScale.x);
			}
			else if (velocity.x < 0.0f)
			{
				visualScale.x = -Mathf.Abs(visualScale.x);
			}

			visual.transform.localScale = visualScale;
		}

		for (int i = 0; i < SPELL_COUNT; ++i)
		{
			if (InputSource.State.FireButtonDown(i))
			{
				Caster.CastSpellBegin(i, InputSource.State.AimDirection, playerManager.GameTime);
			}

			if (InputSource.State.FireButtonUp(i))
			{
				Caster.CastSpellFire(i, InputSource.State.AimDirection, playerManager.GameTime);
			}
		}

		Caster.SpellUpdate(InputSource.State.AimDirection, playerManager.GameTime);

		if (damageable.IsDead && gameObject.activeSelf)
		{
			gameObject.SetActive(false);
		}
	}

	void OnControllerColliderHit(ControllerColliderHit hit)
	{
		if (Vector3.Dot(velocity, hit.normal) < 0.0f)
		{
			velocity = velocity - Vector3.Project(velocity, hit.normal);
			velocity.z = 0.0f;
		}

		Debug.DrawRay(hit.point, hit.normal);

		if (Vector3.Dot(hit.normal, Vector3.up) > floorUpTolerance)
		{
			floorNormal = hit.normal;
			isGrounded = true;
		}
		else if (Mathf.Abs(Vector3.Dot (hit.normal, Vector3.right)) > settings.CosWallAngleTolerance)
		{
			wallNormal = hit.normal;
			isWallSliding = true;
		}
	}

	public object GetCurrentState()
	{
		EnsureInitialized();
		lastState = new PlayerState(transform.position, velocity, damageable.CurrentHealth, null);
		return lastState;
	}

	public void RewindToState(object state)
	{
		if (state == null)
		{
			gameObject.SetActive(false);
		}
		else
		{
			gameObject.SetActive(true);

			lastState = (PlayerState)state;

			transform.position = lastState.Position;
			velocity = lastState.Velocity;

			damageable.CurrentHealth = lastState.Health;

			if (!damageable.IsDead && !gameObject.activeSelf)
			{
				gameObject.SetActive(true);
			}

			if (lastState.InputRecord == null)
			{
				inputSource = new IdleInputSource();
			}
			else
			{
				inputSource = new ReplayInputSource(lastState.InputRecord);
			}
		}
	}

	public TimeManager GetTimeManager()
	{
		return timeManager;
	}

	public bool TeleportTo(Vector3 position)
	{
		Vector3 offsetAmount = transform.TransformDirection(characterController.center);
		Vector3 teleportedPosition = overlapCorrecter.CorrectCapsuleOverlap(position + offsetAmount, Vector3.up, characterController.height, characterController.radius);
		transform.position = teleportedPosition - offsetAmount;
		return true;
	}

	public void DashTo(Vector3 position, float speed, IDashStateDelegate stateDelegate)
	{
		Dictionary<string, System.Object> parameters = new Dictionary<string, System.Object>();

		parameters["position"] = position;
		parameters["speed"] = speed;
		parameters["delegate"] = stateDelegate;

		stateMachine.SetNextState("Dash", parameters);
	}
}
