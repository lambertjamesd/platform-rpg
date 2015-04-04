using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DestroyGameObjectEffect : EffectObject
{
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		GameObject target = instance.GetValue<GameObject>("target", null);
		
		if (target != null)
		{
			TimeManager.DestroyGameObject(target);
		}
	}
}

public class CancelEventEffect : EffectObject
{
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		IEffect target = instance.GetValue<IEffect>("target", null);
		
		if (target != null)
		{
			target.Cancel();
		}
	}
}


public class NumberParameterEffect : EffectObject
{
	string name;
	float defaultValue = 0.0f;

	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		name = instance.GetValue<string>("name", null);
		defaultValue = instance.GetValue<float>("default", 0.0f);
	}

	public override IEffectPropertySource PropertySource
	{
		get
		{
			return new LambdaPropertySource(propertyName => {
				if (propertyName == "result")
				{
					Dictionary<string, SpellDescriptionParameter> parameters = instance.GetContextValue<Dictionary<string, SpellDescriptionParameter>>("parameters", null);

					if (parameters != null && parameters.ContainsKey(name))
					{
						return parameters[name].value;
					}
					else
					{
						return defaultValue;
					}
				}
				else
				{
					return null;
				}
			});
		}
	}
}

public class DelayEffect : EffectObject, IFixedUpdate
{
	private float targetTime;
	private int startSnapshotIndex;
	private UpdateManager updateManager;
	private TimeManager timeManager;

	private float CurrentTime
	{
		get
		{
			return timeManager.CurrentTime;
		}
	}

	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		updateManager = instance.GetContextValue<UpdateManager>("updateManager", null);
		timeManager = instance.GetContextValue<TimeManager>("timeManager", null);
		
		targetTime = CurrentTime + instance.GetValue<float>("duration", 0.0f);
		startSnapshotIndex = timeManager.SnapshotIndex;

		this.AddToUpdateManager(updateManager);
	}

	public void FixedUpdateTick(float dt)
	{
		if (CurrentTime >= targetTime && CurrentTime - dt < targetTime)
		{
			instance.TriggerEvent("timeout", null);

			if (startSnapshotIndex == timeManager.SnapshotIndex)
			{
				// no snapshot happened betweeen the start and end of the timer
				// so it is safe to remove this from the update manager
				this.RemoveFromUpdateManager(updateManager);
			}
		}
	}
}

public class DamageEffect : EffectObject
{
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		float damageAmount = instance.GetValue<float>("amount", 0.0f);
		GameObject gameObject = instance.GetValue<GameObject>("target", null);
		Damageable target = null;

		if (gameObject != null)
		{
			target = gameObject.GetComponent<Damageable>();
		}

		if (target != null)
		{
			if (damageAmount >= 0.0f)
			{
				target.ApplyDamage(damageAmount);
			}
			else
			{
				target.Heal(-damageAmount);
			}
		}
	}
}

public class RepeatEffect : EffectObject
{
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);

		int count = instance.GetIntValue("count", 0);

		for (int i = 0; i < count; ++i)
		{
			GenericPropertySource onRepeatSource = new GenericPropertySource();
			onRepeatSource.AddValue("index", i);

			float normalizedIndex = count <= 1 ? 0.5f : (float)i / (count - 1);
			onRepeatSource.AddValue("normalizedIndex", normalizedIndex);
			onRepeatSource.AddValue("signedIndex", normalizedIndex * 2.0f - 1.0f);
			instance.TriggerEvent("repeat", onRepeatSource);
		}
	}
}

public class IfEffect : EffectObject
{
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		
		GenericPropertySource ifSource = new GenericPropertySource();
		if (instance.GetValue<bool>("condition", false))
		{
			instance.TriggerEvent("true", ifSource);
		}
		else
		{
			instance.TriggerEvent("false", ifSource);
		}
	}
}

public class DebugLogEffect : EffectObject
{
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		Debug.Log(instance.GetValue<object>("input"), instance.Definition.Source);
	}
}

public class GetAncestorEffect : EffectObject
{
	private GameObject result;

	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		GameObject source = instance.GetValue<GameObject>("source", null);
		string name = instance.GetValue<string>("name", null);

		result = null;

		if (source != null)
		{
			result = source.GetParent();

			if (name != null && name.Length != 0)
			{
				while (result != null && result.name != name)
				{
					result = result.GetParent();
				}
			}
		}
	}
	
	
	public override IEffectPropertySource PropertySource
	{
		get
		{
			GenericPropertySource source = new GenericPropertySource();
			source.AddValue("result", result);
			return source;
		}
	}
}

public class CaptureValueEffect : EffectObject {
	private System.Object result;
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		result = instance.GetValue<System.Object>("input", null);
	}
	
	public override IEffectPropertySource PropertySource
	{
		get
		{
			return new LambdaPropertySource(parameterName => {
				switch (parameterName)
				{
				case "result":
					return result;
				}

				return null;
			});
		}
	}
}

public class CountEffect : EffectObject {
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		instance.GetContextValue<CounterEffect>("target", null).Increment();
	}

}

public class CounterEffect : EffectObject, ITimeTravelable {
	private int currentValue = 0;
	private int countTo = 0;
	private bool cancelled = false;
	private TimeManager timeManager;

	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		countTo = instance.GetValue<int>("countTo", 0);
		timeManager = instance.GetContextValue<TimeManager>("timeManager", null);
		timeManager.AddTimeTraveler(this);
	}

	public override IEffectPropertySource PropertySource
	{
		get
		{
			return new LambdaPropertySource(parameterName => {
				switch (parameterName)
				{
				case "currentValue":
					return currentValue;
				case "normalizedValue":
					return (float)currentValue / countTo;
				case "counterCompleted":
					return currentValue >= countTo;
				case "effect":
					return this;
				}
				
				return null;
			});
		}
	}

	public void Increment()
	{
		++currentValue;

		instance.TriggerEvent("count", null);

		if (currentValue == countTo)
		{
			instance.TriggerEvent("completed", null);
		}
	}
	
	public object GetCurrentState()
	{
		return new object[]{
			currentValue,
			cancelled
		};
	}

	public void RewindToState(object state)
	{
		object[] objectArray = (object[])state;
		currentValue = (int)objectArray[0];
		cancelled = (bool)objectArray[1];
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}
}