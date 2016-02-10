using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System;

public interface SpellcastFireListener
{
	void SpellAimed(IEffectPropertySource propertySource);
	void SpellFired(IEffectPropertySource propertySource);
	int Priority { get; }
	bool ConsumeEvent { get; }
}

public class SpellCasterFireEvent : EffectObject, SpellcastFireListener
{	
	private SpellCaster spellCaster = null;
	private int spellIndex = 0;
	private int priority = 0;
	private bool consume = false;
	private IEffectPropertySource lastAimEvent;

	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		GameObject target = instance.GetValue<GameObject>("target", null);

		if (target != null)
		{
			spellCaster = target.GetComponent<SpellCaster>();
			spellIndex = instance.GetIntValue("spellIndex", 0);
			priority = instance.GetIntValue("priority", 0);
			consume = instance.GetValue<bool>("consume", false);

			if (spellCaster != null)
			{
				spellCaster.AddFireListener(this, spellIndex);
			}
		}
	}

	public void SpellAimed(IEffectPropertySource propertySource)
	{
		lastAimEvent = propertySource;
		instance.TriggerEvent("aim", propertySource);
	}

	public void SpellFired(IEffectPropertySource propertySource)
	{
		lastAimEvent = propertySource;
		instance.TriggerEvent("fire", propertySource);
	}

	public override IEffectPropertySource PropertySource
	{
		get
		{
			IEffectPropertySource parentSource = base.PropertySource;
			return new LambdaPropertySource(name => {
				object result = parentSource.GetObject(name);

				if (result != null)
				{
					return result;
				}
				else if (lastAimEvent != null)
				{
					return lastAimEvent.GetObject(name);
				}
				else
				{
					return null;
				}
			});
		}
	}
	
	public int Priority
	{ 
		get
		{
			return priority;
		}
	}

	public bool ConsumeEvent
	{ 
		get
		{
			return consume;
		}
	}

	public override void Cancel()
	{
		if (spellCaster != null)
		{
			spellCaster.RemoveFireListener(this, spellIndex);
		}
	}
}

public class SpellCaster : MonoBehaviour, ITimeTravelable {

	public SpellDescription[] spells;
	private Vector3 forward = Vector3.right;
	
	private struct SpellState
	{
		public float cooldownTime;
		public float startTime;
		public List<SpellcastFireListener> fireListeners;
		public int loopIndex;
		public int chargeCount;
		
		private EffectInstance castInstance;

		public void Init(int chargeCount, float timestamp, float cooldown)
		{
			this.chargeCount = chargeCount;
			cooldownTime = timestamp + cooldown;
		}

		public SpellState Copy()
		{
			SpellState result = new SpellState();

			result.cooldownTime = cooldownTime;
			result.startTime = startTime;
			result.fireListeners = fireListeners.GetRange(0, fireListeners.Count);
			result.loopIndex = loopIndex;
			result.castInstance = castInstance;
			result.chargeCount = chargeCount;

			return result;
		}

		public bool IsFiring
		{
			get
			{
				return castInstance != null;
			}
		}

		public EffectInstance Instance
		{
			get
			{
				return castInstance;
			}
		}

		public void SpellStart(float timestamp, int maxCharges, float cooldown, EffectInstance instance)
		{
			startTime = timestamp;

			if (chargeCount == maxCharges)
			{
				cooldownTime = timestamp + cooldown;
			}
			
			--chargeCount;

			castInstance = instance;
		}

		public int ChargeCount
		{
			get
			{
				return chargeCount;
			}
		}

		public void AddCharge()
		{
			++chargeCount;
		}

		public void ReduceCooldown(float amount)
		{
			cooldownTime -= amount;
		}

		public bool CanUse()
		{
			return chargeCount > 0;
		}

		public void CheckCookdown(float timestep, int maxCharges, float cooldown)
		{
			if (timestep >= cooldownTime && chargeCount < maxCharges)
			{
				++chargeCount;
				cooldownTime += cooldown;
			}
		}

		public void SpellFinish()
		{
			castInstance = null;
		}

		public void AddListener(SpellcastFireListener listener)
		{
			int insertIndex = 0;

			while (insertIndex < fireListeners.Count && fireListeners[insertIndex].Priority >= listener.Priority)
			{
				++insertIndex;
			}

			fireListeners.Insert(insertIndex, listener);
		}

		public void RemoveListener(SpellcastFireListener listener)
		{
			int index = fireListeners.IndexOf(listener);

			if (index != -1)
			{
				fireListeners.RemoveAt(index);

				if (index <= loopIndex)
				{
					--loopIndex;
				}
			}
		}
	}

	private EffectDefinition[] effectDefinitions;
	private EffectInstance[] rootInstances;
	private SpellState[] spellStates;
	private IEffectPropertySource propertySource;
	private TimeManager timeManager;

	// Use this for initialization
	void Awake () {
		IEffectPropertySource gameObjectPropertySource = new GameObjectPropertySource(gameObject, null);
		propertySource = new LambdaPropertySource(name => {
			switch (name)
			{
			case "forward":
				return forward;
			case "team":
				return Player.LayerToTeam(gameObject.layer);
			}
			
			return gameObjectPropertySource.GetObject(name);
		});

		effectDefinitions = new EffectDefinition[spells.Length];
		rootInstances = new EffectInstance[spells.Length];
		spellStates = new SpellState[spells.Length];

		timeManager = gameObject.GetComponentWithAncestors<TimeManager>();
		timeManager.AddTimeTraveler(this);

		Dictionary<string, object> context = new Dictionary<string, object>();

		TilemapOverlapCorrecter overlapCorrector = gameObject.GetComponentWithAncestors<TilemapOverlapCorrecter>();

		context["parentGameObject"] = gameObject.GetParent();
		context["updateManager"] = gameObject.GetComponentWithAncestors<UpdateManager>();
		context["timeManager"] = timeManager;
		context["playerManager"] = gameObject.GetComponentWithAncestors<PlayerManager>();
		context["casterTeam"] = Player.LayerToTeam(gameObject.layer);
		context["gameObject"] = gameObject;
		context["caster"] = this;
		context["spacialIndex"] = overlapCorrector.GetSpacialIndex();

		for (int i = 0; i < spells.Length; ++i)
		{
			Dictionary<string, object> spellContext = new Dictionary<string, object>(context);
			spellContext["parameters"] = spells[i].ParameterMapping;

			EffectParser parser = new EffectParser(spells[i].effect);
			effectDefinitions[i] = parser.Parse();

			rootInstances[i] = new EffectInstance(effectDefinitions[i], propertySource, spellContext);
			spellStates[i].fireListeners = new List<SpellcastFireListener>();
			spellStates[i].Init(spells[i].startingCharges, timeManager.CurrentTime, spells[i].cooldown);
		}
	}
	
	// Update is called once per frame
	void Update () {

	}

	private IEffectPropertySource HoldSource(int spellIndex, Vector3 direction, float timestamp)
	{	
		float holdTime = timestamp - spellStates[spellIndex].startTime;

		return new LambdaPropertySource(name => {
			switch (name)
			{
			case "direction":
				return direction;
			case "position":
				return transform.TransformPoint(spells[spellIndex].castOrigin);
			case "holdTime":
				return holdTime;
			case "holdTimeNormalized":
				return Mathf.Clamp01(holdTime / spells[spellIndex].maxHoldTime);
			case "index":
				return spellIndex;
			}
			
			return null;
		});
	}

	public void CastSpellHold(int spellIndex, Vector3 direction, float timestamp)
	{
		IEffectPropertySource aimSource = HoldSource(spellIndex, direction, timestamp);
		spellStates[spellIndex].Instance.TriggerEvent("aim", aimSource);

		foreach (SpellcastFireListener listener in spellStates[spellIndex].fireListeners)
		{
			listener.SpellAimed(aimSource);
		}
	}

	public void CastSpellBegin(int spellIndex, Vector3 direction, float timestamp)
	{
		if (spellStates[spellIndex].CanUse())
		{
			EffectInstance newInstance = rootInstances[spellIndex].NewContext();
			
			spellStates[spellIndex].SpellStart(timestamp, spells[spellIndex].maxCharges, spells[spellIndex].cooldown, newInstance);

			newInstance.TriggerEvent("begin", new LambdaPropertySource(name => {
				switch (name)
				{
				case "direction":
					return direction;
				case "position":
					return transform.TransformPoint(spells[spellIndex].castOrigin);
				case "index":
					return spellIndex;
				}

				return null;
			}));
		}
	}

	public void AddFireListener(SpellcastFireListener listener, int index)
	{
		spellStates[index].AddListener(listener);
	}
	
	public void RemoveFireListener(SpellcastFireListener listener, int index)
	{
		spellStates[index].RemoveListener(listener);
	}
	
	public void SpellUpdate(Vector3 direction, float timestamp)
	{
		for (int i = 0; i < spells.Length; ++i)
		{
			if (spellStates[i].IsFiring)
			{
				if (timestamp - spellStates[i].startTime > spells[i].maxHoldTime)
				{
					CastSpellFire(i, direction, timestamp);
				}
				else
				{
					CastSpellHold(i, direction, timestamp);
				}
			}

			spellStates[i].CheckCookdown(timestamp, spells[i].maxCharges, spells[i].cooldown);
		}
	}

	public void CastSpellFire(int spellIndex, Vector3 direction, float timestamp)
	{
		if (spellStates[spellIndex].IsFiring)
		{
			IEffectPropertySource propertySource = HoldSource(spellIndex, direction, timestamp);

			// trigger listeners first
			List<SpellcastFireListener> fireListeners = spellStates[spellIndex].fireListeners;
			int currentIndex = 0;
			spellStates[spellIndex].loopIndex = 0;
			int minPriority = int.MinValue;

			while (currentIndex < fireListeners.Count && 
			       fireListeners[currentIndex].Priority >= 0 && 
			       fireListeners[currentIndex].Priority >= minPriority)
			{
				spellStates[spellIndex].loopIndex = currentIndex;

				if (fireListeners[currentIndex].ConsumeEvent)
				{
					minPriority = fireListeners[currentIndex].Priority;
				}

				fireListeners[currentIndex].SpellFired(propertySource);

				currentIndex = spellStates[spellIndex].loopIndex + 1;
			}

			if (minPriority <= 0)
			{
				Debug.Log("Fired spell " + spells[spellIndex].name);
				spellStates[spellIndex].Instance.TriggerEvent("fire", propertySource);
			}
			
			while (currentIndex < fireListeners.Count && fireListeners[currentIndex].Priority >= minPriority)
			{
				spellStates[spellIndex].loopIndex = currentIndex;

				if (fireListeners[currentIndex].ConsumeEvent)
				{
					minPriority = fireListeners[currentIndex].Priority;
				}

				fireListeners[currentIndex].SpellFired(propertySource);
				
				currentIndex = spellStates[spellIndex].loopIndex + 1;
			}

			spellStates[spellIndex].SpellFinish();
		}
	}

	public float RandomFloat(int hashModifier)
	{
		MD5 rndGen = new MD5CryptoServiceProvider();
		byte[] hashInput = BitConverter.GetBytes(timeManager.CurrentTime)
			.Concat(BitConverter.GetBytes(GetHashCode()))
			.Concat(BitConverter.GetBytes(hashModifier))
			.ToArray();
		return (float)BitConverter.ToInt32(rndGen.ComputeHash(hashInput), 0) / ((float)0x10000 * (float)0x10000);
	}

	public void ResetCooldown(int spellIndex, float time)
	{
		if (time == 0.0f)
		{
			if (spellStates[spellIndex].ChargeCount < spells[spellIndex].maxCharges)
			{
				spellStates[spellIndex].AddCharge();
			}
		}
		else
		{
			spellStates[spellIndex].ReduceCooldown(time);
		}
	}

	public int GetSpellCount()
	{
		return spells.Length;
	}

	public SpellDescription GetSpell(int index)
	{
		return spells[index];
	}

	public float GetSpellCooldown(int index)
	{
		return spellStates[index].cooldownTime;
	}

	public int GetChargeCount(int index)
	{
		return spellStates[index].ChargeCount;
	}
	
	public object GetCurrentState()
	{
		if (gameObject.activeSelf)
		{
			List<SpellState> result = new List<SpellState>();

			foreach (SpellState state in spellStates)
			{
				result.Add(state.Copy());
			}

			return result;
		}
		else
		{
			return null;
		}
	}

	public void RewindToState(object state)
	{
		if (state != null)
		{
			spellStates = ((List<SpellState>)state).ToArray();
		}
		else
		{
			Destroy(gameObject);
		}
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}

	public Vector3 Forward
	{
		get
		{
			return forward;
		}

		set
		{
			forward = value;
		}
	}
}
