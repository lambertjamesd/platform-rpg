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
			// ask the object to destroy itself, it it doesn't
			// then just destroy the game object normally
			if (!SelfDestruct.DestroySelf(target))
			{
				TimeManager.DestroyGameObject(target);
			}
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

public interface IDelayEffect : IFixedUpdate, ITimeTravelable
{
	float RemainingTime { get; }
}

public class DelayEffectCommon
{
	private EffectInstance instance;
	private float remainingTime;
	private bool addedToUpdateManager;
	private UpdateManager updateManager;
	private TimeManager timeManager;
	private IDelayEffect delayEffect;

	private void AddToUpdate()
	{
		if (!addedToUpdateManager)
		{
			delayEffect.AddToUpdateManager(updateManager);
			addedToUpdateManager = true;
		}
	}
	
	private void RemoveFromUpdate()
	{
		if (addedToUpdateManager)
		{
			delayEffect.RemoveFromUpdateManager(updateManager);
			addedToUpdateManager = false;
		}
		
	}
	
	public void StartEffect(EffectInstance instance, IDelayEffect delayEffect) {
		this.instance = instance;
		this.delayEffect = delayEffect;
		updateManager = instance.GetContextValue<UpdateManager>("updateManager", null);
		timeManager = instance.GetContextValue<TimeManager>("timeManager", null);
		timeManager.AddTimeTraveler(delayEffect);
		
		remainingTime = instance.GetValue<float>("duration", 0.0f);
		
		AddToUpdate();
	}

	public void Cancel()
	{
		remainingTime = 0.0f;
		RemoveFromUpdate();
	}
	
	public void FixedUpdateTick(float dt)
	{
		if (remainingTime > 0.0f)
		{
			remainingTime -= dt;
			
			if (remainingTime <= 0.0f)
			{
				instance.TriggerEvent("timeout", null);
				RemoveFromUpdate();
			}
		}
	}
	
	public float RemainingTime
	{
		get
		{
			return remainingTime;
		}
	}
	
	public object GetCurrentState()
	{
		return remainingTime > 0.0f ? (object)remainingTime : null;
	}
	
	public void RewindToState(object state)
	{
		if (state == null)
		{
			RemoveFromUpdate();
		}
		else
		{
			remainingTime = (float)state;
			
			if (remainingTime > 0.0f)
			{
				AddToUpdate();
			}
		}
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}
}

public class DelayEffect : EffectObject, IDelayEffect, IFixedUpdate, ITimeTravelable
{
	private DelayEffectCommon delayCommon = new DelayEffectCommon();

	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		delayCommon.StartEffect(instance, this);
	}
	
	public void FixedUpdateTick(float dt)
	{
		delayCommon.FixedUpdateTick(dt);
	}
	
	public override void Cancel ()
	{
		delayCommon.Cancel();
	}

	public float RemainingTime
	{
		get
		{
			return delayCommon.RemainingTime;
		}
	}

	public object GetCurrentState()
	{
		return delayCommon.GetCurrentState();
	}

	public void RewindToState(object value)
	{
		delayCommon.RewindToState(value);
	}

	public TimeManager GetTimeManager()
	{
		return delayCommon.GetTimeManager();
	}
}

public class DelayGameObjectEffect : EffectGameObject, IDelayEffect, IFixedUpdate, ITimeTravelable
{
	private DelayEffectCommon delayCommon = new DelayEffectCommon();
	
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		delayCommon.StartEffect(instance, this);

		float radius = instance.GetValue<float>("radius", 0.0f);

		if (gameObject.GetComponent<Collider>() == null && radius > 0.0f)
		{
			CapsuleCollider capsule = gameObject.GetOrAddComponent<CapsuleCollider>();

			capsule.radius = radius;
			capsule.height = instance.GetValue<float>("height", 0.0f);
			capsule.center = instance.GetValue<Vector3>("center", Vector3.zero);
		}
	}

	public override void Cancel ()
	{
		delayCommon.Cancel();
	}

	public void FixedUpdateTick(float dt)
	{
		delayCommon.FixedUpdateTick(dt);
	}
	
	public float RemainingTime
	{
		get
		{
			return delayCommon.RemainingTime;
		}
	}
	
	public object GetCurrentState()
	{
		return delayCommon.GetCurrentState();
	}
	
	public void RewindToState(object value)
	{
		if (value == null)
		{
			Destroy(gameObject);
		}
		else
		{
			delayCommon.RewindToState(value);
		}
	}
	
	public TimeManager GetTimeManager()
	{
		return delayCommon.GetTimeManager();
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
		}
	}
}

public class HealEffect : EffectObject
{
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		float healAmount = instance.GetValue<float>("amount", 0.0f);
		GameObject gameObject = instance.GetValue<GameObject>("target", null);
		Damageable target = null;
		
		if (gameObject != null)
		{
			target = gameObject.GetComponent<Damageable>();
		}
		
		if (target != null)
		{
			if (healAmount >= 0.0f)
			{
				target.Heal(healAmount);
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

public class ForeachEffect : EffectObject
{
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);

		object enumerable = instance.GetValue<object>("elements", null);
		IEnumerable elements = (IEnumerable)enumerable;

		if (elements != null)
		{
			int index = 0;
			foreach (object element in elements)
			{
				instance.TriggerEvent("emit", new LambdaPropertySource(propertyName => {
					if (propertyName == "element")
					{
						return element;
					}
					else if (propertyName == "index")
					{
						return index;
					}

					return null;
				}));
				++index;
			}
		}
	}
}

public class UpdateEffect : EffectObject, IFixedUpdate, ITimeTravelable
{
	private UpdateManager updateManager;
	private TimeManager timeManager;

	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);

		updateManager = instance.GetContextValue<UpdateManager>("updateManager", null);
		updateManager.AddReciever(this);
		timeManager = instance.GetContextValue<TimeManager>("timeManager", null);
		timeManager.AddTimeTraveler(this);
	}
	
	public override void Cancel()
	{
		if (updateManager != null)
		{
			updateManager.RemoveReciever(this);
			updateManager = null;
		}
	}
	
	public void FixedUpdateTick(float timestep)
	{
		instance.TriggerEvent("frame", new LambdaPropertySource(name => {
			switch (name)
			{
			case "timestep":
				return timestep;
			}

			return null;
		}));
	}

	
	public object GetCurrentState()
	{
		return updateManager;
	}

	public void RewindToState(object state)
	{
		updateManager = (UpdateManager)state;

		if (updateManager != null)
		{
			updateManager.AddReciever(this);
		}
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}
}

public class DebugLogEffect : EffectObject
{
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);

		object input = instance.GetValue<object>("input", "");

		if (input == null)
		{
			Debug.Log(instance.GetValue<string>("label", "unlabelled") + ": null", instance.Definition.Source);
		}
		else
		{
			Debug.Log(instance.GetValue<string>("label", "unlabelled") + ": " + input.ToString(), instance.Definition.Source);
		}
	}
}

public class DebugLineEffect : EffectObject
{
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);

		Debug.DrawLine(
			instance.GetValue<Vector3>("start", Vector3.zero),
			instance.GetValue<Vector3>("end",Vector3.zero),
			instance.GetValue<Color>("color", Color.green),
			instance.GetValue<float>("duration", 0.0f)
		);
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
		CounterEffect target = instance.GetValue<CounterEffect>("target", null);
		object value = instance.GetValue<object>("element", null);

		if (instance.GetValue<bool>("decrement", false))
		{
			target.Decrement(value);
		}
		else
		{
			target.Increment(value);
		}
	}

}

public class CounterEffect : EffectObject, ITimeTravelable {
	private int currentValue = 0;
	private int countTo = 0;
	private bool cancelled = false;
	private TimeManager timeManager;
	private List<object> elements = new List<object>();

	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		countTo = instance.GetValue<int>("countTo", int.MaxValue);
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
				case "cancelled":
					return cancelled;
				case "effect":
					return this;
				case "elements":
					return elements;
				}
				
				return null;
			});
		}
	}

	public override void Cancel()
	{
		if (!cancelled && currentValue < countTo)
		{
			cancelled = true;
			instance.TriggerEvent("cancelled", null);
			instance.TriggerEvent("ended", null);
		}
	}

	public void Increment(object element)
	{
		if (!cancelled)
		{
			++currentValue;
			elements.Add(element);

			instance.TriggerEvent("count", null);

			if (currentValue == countTo)
			{
				instance.TriggerEvent("completed", null);
				instance.TriggerEvent("ended", null);
			}
		}
	}
	
	public void Decrement(object element)
	{
		if (!cancelled)
		{
			--currentValue;
			elements.Remove(element);
			
			instance.TriggerEvent("count", null);
		}
	}
	
	public object GetCurrentState()
	{
		if (!cancelled)
		{
			return new object[]{
				currentValue,
				new List<object>(elements)
			};
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
			object[] objectArray = (object[])state;
			currentValue = (int)objectArray[0];
			elements = (List<object>)objectArray[1];
		}
		else
		{
			cancelled = true;
		}
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}
}

public class SetPositionEffect : EffectObject {
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		GameObject target = instance.GetValue<GameObject>("target", null);

		if (target != null) {
			GameObject parent = instance.GetValue<GameObject>("parent", target.GetParent());
			target.transform.parent = parent ? parent.transform : null;
			target.transform.position = instance.GetValue<Vector3>("position", target.transform.position);
		}
	}
}

public class SetLayerEffect : EffectObject {
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		GameObject target = instance.GetValue<GameObject>("target", null);
		
		if (target != null) {
			target.layer = instance.GetValue<int>("layer", target.layer);
		}
	}
}