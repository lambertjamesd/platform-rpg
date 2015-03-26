using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public interface SpellcastFireListener
{
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

	public void SpellFired(IEffectPropertySource propertySource)
	{
		instance.TriggerEvent("fire", propertySource);
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
		base.Cancel();
		
		if (spellCaster != null)
		{
			spellCaster.RemoveFireListener(this, spellIndex);
		}
	}
}

public class SpellCaster : MonoBehaviour, ITimeTravelable {

	public SpellDescription[] spells;
	
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
	private GameObjectPropertySource propertySource;
	private TimeManager timeManager;

	// Use this for initialization
	void Start () {
		propertySource = new GameObjectPropertySource(gameObject);

		effectDefinitions = new EffectDefinition[spells.Length];
		rootInstances = new EffectInstance[spells.Length];
		spellStates = new SpellState[spells.Length];

		Dictionary<string, object> context = new Dictionary<string, object>();

		timeManager = gameObject.GetComponentWithAncestors<TimeManager>();

		context["parentGameObject"] = gameObject.GetParent();
		context["updateManager"] = gameObject.GetComponentWithAncestors<UpdateManager>();
		context["timeManager"] = timeManager;
		context["playerManager"] = gameObject.GetComponentWithAncestors<PlayerManager>();

		timeManager.AddTimeTraveler(this);

		for (int i = 0; i < spells.Length; ++i)
		{
			EffectParser parser = new EffectParser(spells[i].effect);
			effectDefinitions[i] = parser.Parse();

			rootInstances[i] = new EffectInstance(effectDefinitions[i], propertySource, context);
			spellStates[i].fireListeners = new List<SpellcastFireListener>();
		}
	}
	
	// Update is called once per frame
	void Update () {

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
			if (spellStates[i].isFiring && timestamp - spellStates[i].startTime > spells[i].maxHoldTime)
			{
				CastSpellFire(i, direction, timestamp);
			}
		}
	}

	public void CastSpellFire(int spellIndex, Vector3 direction, float timestamp)
	{
		if (spellStates[spellIndex].isFiring)
		{
			float holdTime = timestamp - spellStates[spellIndex].startTime;

			IEffectPropertySource propertySource = new LambdaPropertySource(name => {
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
		return spellStates.Select(spellState => spellState.Copy());
	}

	public void RewindToState(object state)
	{
		spellStates = ((IEnumerable<SpellState>)state).ToArray();
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}
}
