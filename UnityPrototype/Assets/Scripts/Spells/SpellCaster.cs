using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
		public bool isFiring;
		public List<SpellcastFireListener> fireListeners;
		public int loopIndex;

		public SpellState Copy()
		{
			SpellState result = new SpellState();

			result.cooldownTime = cooldownTime;
			result.startTime = startTime;
			result.isFiring = isFiring;
			result.fireListeners = fireListeners.GetRange(0, fireListeners.Count);
			result.loopIndex = loopIndex;

			return result;
		}

		public void SpellStart(float timestamp, float cooldown)
		{
			startTime = timestamp;
			cooldownTime = timestamp + cooldown;
			isFiring = true;
		}

		public bool CanUse(float timestamp)
		{
			return timestamp >= cooldownTime;
		}

		public void SpellFinish()
		{
			isFiring = false;
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
			}
			
			return gameObjectPropertySource.GetObject(name);
		});

		effectDefinitions = new EffectDefinition[spells.Length];
		rootInstances = new EffectInstance[spells.Length];
		spellStates = new SpellState[spells.Length];

		timeManager = gameObject.GetComponentWithAncestors<TimeManager>();
		timeManager.AddTimeTraveler(this);

		Dictionary<string, object> context = new Dictionary<string, object>();

		context["parentGameObject"] = gameObject.GetParent();
		context["updateManager"] = gameObject.GetComponentWithAncestors<UpdateManager>();
		context["timeManager"] = timeManager;
		context["playerManager"] = gameObject.GetComponentWithAncestors<PlayerManager>();
		context["casterTeam"] = Player.LayerToTeam(gameObject.layer);
		context["gameObject"] = gameObject;

		for (int i = 0; i < spells.Length; ++i)
		{
			Dictionary<string, object> spellContext = new Dictionary<string, object>(context);
			spellContext["parameters"] = spells[i].ParameterMapping;

			EffectParser parser = new EffectParser(spells[i].effect);
			effectDefinitions[i] = parser.Parse();

			rootInstances[i] = new EffectInstance(effectDefinitions[i], propertySource, spellContext);
			spellStates[i].fireListeners = new List<SpellcastFireListener>();
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
		rootInstances[spellIndex].TriggerEvent("aim", aimSource);

		foreach (SpellcastFireListener listener in spellStates[spellIndex].fireListeners)
		{
			listener.SpellAimed(aimSource);
		}
	}

	public void CastSpellBegin(int spellIndex, Vector3 direction, float timestamp)
	{
		if (spellStates[spellIndex].CanUse(timestamp))
		{
			rootInstances[spellIndex].TriggerEvent("begin", new LambdaPropertySource(name => {
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

			spellStates[spellIndex].SpellStart(timestamp, spells[spellIndex].cooldown);
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
			if (spellStates[i].isFiring)
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
		}
	}

	public void CastSpellFire(int spellIndex, Vector3 direction, float timestamp)
	{
		if (spellStates[spellIndex].isFiring)
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
				rootInstances[spellIndex].TriggerEvent("fire", propertySource);
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
	
	public object GetCurrentState()
	{
		List<SpellState> result = new List<SpellState>();

		foreach (SpellState state in spellStates)
		{
			result.Add(state.Copy());
		}

		return result;
	}

	public void RewindToState(object state)
	{
		spellStates = ((List<SpellState>)state).ToArray();
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
