using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerState
{
	private Vector3 position;
	private Vector3 velocity;
	private float health;
	private InputRecording input;
	private object animationState;
	private object stateMachineState;

	public PlayerState(Vector3 position, Vector3 velocity, float health, InputRecording input, object animationState, object stateMachineState)
	{
		this.position = position;
		this.velocity = velocity;
		this.health = health;
		this.input = input;
		this.animationState = animationState;
		this.stateMachineState = stateMachineState;
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

	public object AnimationState
	{
		get
		{
			return animationState;
		}
	}

	public object StateMachineState
	{
		get
		{
			return stateMachineState;
		}
	}
}

public class PlayerStatus
{
	public bool isRooted;

	public bool CanCastSpell(SpellDescription description)
	{
		if (isRooted && description.blockedWhenRooted)
		{
			return false;
		}

		return true;
	}
}

public class Player : MonoBehaviour, IFixedUpdate, ITimeTravelable, ITeleportable, IDashable, IRootable {

	public MoveState moveState;
	public JumpState jumpState;
	public FreeFallState freefallState;
	public WallSlideState wallSlideState;
	public DashState dashState;
	public RootedState rootedState;

	public int team;

	public PlayerSettings settings;
	private PlayerStats stats;

	public KnockbackHandler knockback;

	public GameObject visual;

	private PlayerStatus status = new PlayerStatus();

	private Animator visualAnimator;
	private AnimatorStateSaver animatorStateSaver;

	private TilemapOverlapCorrecter overlapCorrecter;
	private UpdateManager updateManager;
	private TimeManager timeManager;

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


	public PlayerStatus Status
	{
		get
		{
			return status;
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

	public void DefaultMovement(float timestep)
	{
		Move (Velocity * timestep + 0.5f * timestep * timestep * Physics.gravity);
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
				if (!visual.transform.IsChildOf(transform))
				{
					visual = (GameObject)Instantiate(visual);
					visual.transform.parent = transform;
					visual.transform.localPosition = Vector3.zero;
					visual.transform.localRotation = Quaternion.identity;
				}

				visualAnimator = visual.GetComponent<Animator>();
				animatorStateSaver = new AnimatorStateSaver(visualAnimator, new string[]{

				}, new string[]{

				}, new string[]{

				});
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
				case "Rooted":
					return rootedState;
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
		return Mathf.Max(0.0f, spellCaster.GetSpellCooldown(index) - timeManager.CurrentTime);
	}

	public void OnEnable()
	{	
		updateManager = updateManager ?? gameObject.GetComponentWithAncestors<UpdateManager>();
		timeManager = timeManager ?? gameObject.GetComponentWithAncestors<TimeManager>();

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
			SpellDescription description = Caster.GetSpell(i);

			if (InputSource.State.FireButtonDown(i) && Status.CanCastSpell(description))
			{
				Caster.CastSpellBegin(i, InputSource.State.AimDirection, timeManager.CurrentTime);
			}

			if (InputSource.State.FireButtonUp(i))
			{
				Caster.CastSpellFire(i, InputSource.State.AimDirection, timeManager.CurrentTime);
			}
		}

		Caster.SpellUpdate(InputSource.State.AimDirection, timeManager.CurrentTime);

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
		object animationState = animatorStateSaver == null ? null : animatorStateSaver.GetCurrentState();
		lastState = new PlayerState(transform.position, velocity, damageable.CurrentHealth, null, animationState, stateMachine.GetCurrentState());
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

			if (animatorStateSaver != null)
			{
				animatorStateSaver.RewindToState(lastState.AnimationState);
			}

			stateMachine.RewindToState(lastState.StateMachineState);
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

		stateMachine.SetNextState("Dash", parameters, 1);
	}

	public void Root(IRootedStateDelegate rootDeletate)
	{
		Dictionary<string, System.Object> parameters = new Dictionary<string, System.Object>();

		parameters["delegate"] = rootDeletate;
		
		stateMachine.SetNextState("Rooted", parameters, 2);
	}
}
