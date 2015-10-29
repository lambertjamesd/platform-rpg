using UnityEngine;
using System.Collections;

public class StoreValueEffect : EffectObject {
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		instance.GetValue<ValueStoreEffect>("target", null).SetValue(instance.GetValue<object>("value", null));
	}
	
}

public class ValueStoreEffect :  EffectObject, ITimeTravelable {
	private object currentValue;
	private TimeManager timeManager;
	
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
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
				case "effect":
					return this;
				}
				
				return null;
			});
		}
	}
	
	public override void Cancel()
	{
		SetValue(null);
	}

	private static bool AreEqual(object a, object b)
	{
		return a == b ||
			(a != null && b != null &&
			 a.Equals(b));
	}
	
	public void SetValue(object newValue)
	{
		if (!AreEqual(newValue, currentValue))
		{
			GenericPropertySource propertySource = new GenericPropertySource();
			propertySource.AddValue("newValue", newValue);
			propertySource.AddValue("oldValue", currentValue);

			instance.TriggerEvent("change", propertySource);

			currentValue = newValue;
		}
	}
	
	public object GetCurrentState()
	{
		return currentValue;
	}
	
	public void RewindToState(object state)
	{
		currentValue = state;
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}
}