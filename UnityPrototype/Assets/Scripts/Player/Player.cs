﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerState
{
	private Vector3 position;
	private Vector3 velocity;
	private float forwardX;
	private float health;
	private InputRecording input;
	private object animationState;
	private object stateMachineState;
	private object statsState;
	private InputState currentInputState;
	private List<InputScrambler> scramblers;

	public PlayerState(Vector3 position, 
	                   Vector3 velocity, 
	                   float forwardX,
	                   float health, 
	                   InputRecording input, 
	                   object animationState, 
	                   object stateMachineState, 
	                   object statsState, 
	                   InputState currentInputState,
	                   List<InputScrambler> scramblers)
	{
		this.position = position;
		this.velocity = velocity;
		this.forwardX = forwardX;
		this.health = health;
		this.input = input;
		this.animationState = animationState;
		this.stateMachineState = stateMachineState;
		this.statsState = statsState;
		this.currentInputState = currentInputState;
		this.scramblers = scramblers;
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

	public float ForwardX
	{
		get
		{
			return forwardX;
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

	public object StatsState
	{
		get
		{
			return statsState;
		}
	}

	public InputState CurrentInputState
	{
		get
		{
			return currentInputState;
		}
	}

	public List<InputScrambler> InputScramblers
	{
		get
		{
			return scramblers;
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
			return true;//false;
		}

		return true;
	}
}

public class InputScrambler
{
	private Vector2 rotation;
	private bool flipX;

	public InputScrambler(Vector2 rotation, bool flipX)
	{
		this.rotation = rotation.normalized;
		this.flipX = flipX;
	}

	public Vector2 Scramble(Vector2 input)
	{
		Vector2 result = new Vector2(input.x * rotation.x - input.y * rotation.y, input.y * rotation.x + input.x * rotation.y);

		if (flipX)
		{
			result.x *= -1.0f;
		}

		return result;
	}
}

public class Player : MonoBehaviour, IFixedUpdate, ITimeTravelable, ITeleportable, IDashable, IRootable {

	public MoveState moveState;
	public JumpState jumpState;
	public FreeFallState freefallState;
	public WallSlideState wallSlideState;
	public DashState dashState;
	public RootedState rootedState;

	private int team;
	private int playerIndex;
	private int turnOrder;
	
	[Multiline]
	public string description;
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

	private InputState currentInputState = new InputState(null);
	private IInputSource inputSource;

	private StateMachine stateMachine;
	private CustomCharacterController characterController;
	private SpellCaster spellCaster;
	private Damageable damageable;
	private Vector3 velocity;
	private Vector3 floorNormal = Vector3.up;
	private float floorUpTolerance;

	private bool isGrounded;
	
	private Vector3 wallNormal;
	private bool isWallSliding;

	private PlayerState lastState;

	private List<InputScrambler> inputScramblers = new List<InputScrambler>();

	public static readonly int SPELL_COUNT = 3;
	private static int firstTeamLayer = 8;
	private static int layerOffsetPerTeam = 3;

	public static int TeamToLayer(int team)
	{
		return team * layerOffsetPerTeam + firstTeamLayer;
	}

	public static int LayerToTeam(int layer)
	{
		return (layer - firstTeamLayer) / layerOffsetPerTeam;
	}

	public SpellCaster Caster
	{
		get
		{
			return spellCaster;
		}
	}

	public InputState CurrentInputState
	{
		get
		{
			return currentInputState;
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

	public int Team
	{
		get
		{
			return team;
		}

		set
		{
			team = value;
			gameObject.layer = firstTeamLayer + team * layerOffsetPerTeam;

			if (characterController != null)
			{
				characterController.collisionLayers = CollisionLayers.AllyLayers(team);
				characterController.UpdateProperties();
			}
			/*SpriteRenderer[] renderers = gameObject.GetComponentsInChildren<SpriteRenderer>(renderer);
			foreach (Renderer childRenderer in renderers)
			{
				childRenderer.material.color = TeamColors.GetColor(team);
			}*/
		}
	}

	public int PlayerIndex
	{
		get; set;
	}

	public static int GetTurnOrder(GameObject source)
	{
		Player player = source.GetComponent<Player>();
	
		if (player == null)
		{
			return int.MaxValue;
		}
		else
		{
			return player.turnOrder;
		}
	}
	
	public int TurnOrder
	{
		get
		{
			return turnOrder;
		}
	}

	public PlayerManager Players
	{
		get; set;
	}

	public void StartTurn(int turnOrder)
	{
		this.turnOrder = turnOrder;
	}

	public void EndTurn()
	{

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
		isGrounded = false;
		isWallSliding = false;

		DeterminismDebug.GetSingleton().Log(gameObject, transform.position.x);
		DeterminismDebug.GetSingleton().Log(gameObject, transform.position.y);
		DeterminismDebug.GetSingleton().Log(gameObject, velocity.x);
		DeterminismDebug.GetSingleton().Log(gameObject, velocity.y);
		DeterminismDebug.GetSingleton().Log(gameObject, amount.x);
		DeterminismDebug.GetSingleton().Log(gameObject, amount.y);
		DeterminismDebug.GetSingleton().Log(gameObject, amount.z);
		characterController.Move(new Vector2(amount.x, amount.y));
		DeterminismDebug.GetSingleton().Log(gameObject, transform.position.x);
		DeterminismDebug.GetSingleton().Log(gameObject, transform.position.y);
		DeterminismDebug.GetSingleton().Log(gameObject, velocity.x);
		DeterminismDebug.GetSingleton().Log(gameObject, velocity.y);
		DeterminismDebug.GetSingleton().Log(gameObject, isGrounded.ToString());
		DeterminismDebug.GetSingleton().Log(gameObject, isWallSliding.ToString());
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
			characterController = GetComponent<CustomCharacterController>();
			spellCaster = GetComponent<SpellCaster>();
			damageable = GetComponent<Damageable>();
			floorUpTolerance = Mathf.Cos(characterController.slopeLimit * Mathf.Deg2Rad);
			overlapCorrecter = gameObject.GetComponentWithAncestors<TilemapOverlapCorrecter>();
			stats = gameObject.GetComponent<PlayerStats>();
			characterController.AddToIndex(overlapCorrecter.GetSpacialIndex());
			characterController.collisionLayers = CollisionLayers.AllyLayers(team);
			characterController.moveCollisionLayers = CollisionLayers.ObstacleLayers;
			characterController.UpdateProperties();

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
				animatorStateSaver = new AnimatorStateSaver(visualAnimator);
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

	public void LastFrame()
	{

	}

	public void StartRecording()
	{
		EnsureInitialized();

		lastState.InputRecord = new InputRecording();
		inputSource = new RecordInputSource(lastState.InputRecord, new ControllerInputSource(transform));
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

	public int GetChargeCount(int index)
	{
		return spellCaster.GetChargeCount(index);
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

	public Vector3 Forward
	{
		get
		{
			return spellCaster.Forward;
		}

		set
		{
			spellCaster.Forward = value.x > 0.0f ? Vector3.right : Vector3.left;
		}
	}

	public void Update()
	{
		VisualAnimator.speed = updateManager.SpeedModifierForTarget(this);
	}

	public void FixedUpdateTick(float timestep)
	{
		inputSource.FrameStart(currentInputState);
		currentInputState = inputSource.State;

		DeterminismDebug.GetSingleton().Log(gameObject, transform.position.x);
		DeterminismDebug.GetSingleton().Log(gameObject, transform.position.y);
		DeterminismDebug.GetSingleton().Log(gameObject, transform.position.z);
		DeterminismDebug.GetSingleton().Log(gameObject, velocity.x);
		DeterminismDebug.GetSingleton().Log(gameObject, velocity.y);
		DeterminismDebug.GetSingleton().Log(gameObject, velocity.z);

		Vector2 direction = new Vector2(currentInputState.AimDirection.x, currentInputState.AimDirection.y);

		foreach (InputScrambler scrambler in inputScramblers)
		{
			direction = scrambler.Scramble(direction);
		}

		currentInputState = currentInputState.WithNewAim(direction.x, new Vector3(direction.x, direction.y, currentInputState.AimDirection.z));

		stateMachine.Update(timestep);

		if (visualAnimator != null)
		{
			Vector3 visualScale = visual.transform.localScale;

			visualAnimator.SetBool("Grounded", IsGrounded);
			visualAnimator.SetFloat("Speed", Mathf.Max(Mathf.Abs(velocity.x), 0.05f));
			visualAnimator.SetFloat("YVelocity", velocity.y / Mathf.Abs(visualScale.y));

			if (velocity.x != 0.0f)
			{
				Forward = velocity;

				visualScale.x = (velocity.x > 0.0f) ? Mathf.Abs(visualScale.x) : -Mathf.Abs(visualScale.x);
			}

			visual.transform.localScale = visualScale;
		}

		for (int i = 0; i < SPELL_COUNT; ++i)
		{
			SpellDescription description = Caster.GetSpell(i);

			if (CurrentInputState.FireButtonDown(i) && Status.CanCastSpell(description))
			{
				Caster.CastSpellBegin(i, CurrentInputState.AimDirection, timeManager.CurrentTime);
			}

			if (CurrentInputState.FireButtonUp(i))
			{
				Caster.CastSpellFire(i, CurrentInputState.AimDirection, timeManager.CurrentTime);
			}
		}

		Caster.SpellUpdate(CurrentInputState.AimDirection, timeManager.CurrentTime);

		if (damageable.IsDead && gameObject.activeSelf)
		{
			gameObject.SetActive(false);
		}
	}

	private void HandleHit(Vector3 normal)
	{
		DeterminismDebug.GetSingleton().Log(gameObject, normal.x);
		DeterminismDebug.GetSingleton().Log(gameObject, normal.y);
		DeterminismDebug.GetSingleton().Log(gameObject, normal.z);
		
		if (Vector3.Dot(velocity, normal) < 0.0f)
		{
			ApplyFallingDamage(new Vector2(velocity.x, velocity.y), new Vector2(normal.x, normal.y));
			
			velocity = velocity - Vector3.Project(velocity, normal);
			velocity.z = 0.0f;
		}
		
		if (Vector3.Dot(normal, Vector3.up) > floorUpTolerance)
		{
			floorNormal = normal;
			isGrounded = true;
		}
		else if (Mathf.Abs(Vector3.Dot (normal, Vector3.right)) > settings.CosWallAngleTolerance)
		{
			wallNormal = normal;
			isWallSliding = true;
		}
	}

	void OnCustomControllerHit(ShapeRaycastHit hit)
	{
		HandleHit(new Vector3(hit.Normal.x, hit.Normal.y));
	}

	public void Playback(InputRecording recording)
	{
		if (lastState != null)
		{
			lastState.InputRecord = recording;
		}

		inputSource = new ReplayInputSource(recording, null);
	}

	public InputRecording LastRecording
	{
		get
		{
			return lastState == null ? null : lastState.InputRecord;
		}
	}

	public object GetCurrentState()
	{
		if (gameObject.activeSelf)
		{
			EnsureInitialized();
			object animationState = animatorStateSaver == null ? null : animatorStateSaver.GetCurrentState();
			lastState = new PlayerState(transform.position, 
			                            velocity, 
			                            Forward.x,
			                            damageable.CurrentHealth, 
			                            null, 
			                            animationState, 
			                            stateMachine.GetCurrentState(), 
			                            stats.GetCurrentState(), 
			                            currentInputState,
			                            new List<InputScrambler>(inputScramblers));
			return lastState;
		}
		else
		{
			return null;
		}
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
			Forward = new Vector3(lastState.ForwardX, 0.0f);

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
				inputSource = new ReplayInputSource(lastState.InputRecord, transform);
			}

			if (animatorStateSaver != null)
			{
				animatorStateSaver.RewindToState(lastState.AnimationState);
			}

			stats.RewindToState(lastState.StatsState);

			stateMachine.RewindToState(lastState.StateMachineState);

			currentInputState = lastState.CurrentInputState;

			inputScramblers = new List<InputScrambler>(lastState.InputScramblers);

			characterController.UpdateIndex();
		}
	}

	public TimeManager GetTimeManager()
	{
		return timeManager;
	}

	public void ApplyFallingDamage(Vector2 speed, Vector2 normal)
	{
		float minFallDistance = stats.GetNumberStat("minFallDamageDistance", 12.0f);
		float maxFallDistance = stats.GetNumberStat("maxFallDamageDistance", 30.0f);

		float minFallDamage = stats.GetNumberStat("minFallDamage", 0.02f) * damageable.maxHealth;
		float maxFallDamage = stats.GetNumberStat("maxFallDamage", 0.1f) * damageable.maxHealth;

		float damageRatio = PathingMath.FallingDamageRatio(velocity, normal, Physics.gravity.y, minFallDistance, maxFallDistance);

		if (damageRatio > 0.0f)
		{
			float damage = Mathf.Lerp(minFallDamage, maxFallDamage, damageRatio);

			damageable.ApplyDamage(damage);
		}
	}

	public bool TeleportTo(Vector3 position)
	{
		Vector3 offsetAmount = transform.TransformDirection(new Vector3(characterController.offset.x, characterController.offset.y));
		Vector3 teleportedPosition = overlapCorrecter.CorrectCapsuleOverlap(
			position + offsetAmount, 
			Vector3.up, 
			characterController.innerHeight + characterController.radius * 2.0f, 
			characterController.radius
		);
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

	public void AddScrambler(InputScrambler scrambler)
	{
		inputScramblers.Add(scrambler);
	}

	public void RemoveScrambler(InputScrambler scrambler)
	{
		inputScramblers.Remove(scrambler);
	}
}
