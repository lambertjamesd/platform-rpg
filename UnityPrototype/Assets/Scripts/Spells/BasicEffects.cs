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
			target.ApplyDamage(damageAmount);
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
