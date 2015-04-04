using UnityEngine;
using System.Collections;

public interface IRootable {
	void Root(IRootedStateDelegate rootDelegate);
}

public class RootEffect : EffectObject, IRootedStateDelegate
{
	private float timeOfFreedom = float.MaxValue;
	private TimeManager timeManager;

	public override void StartEffect (EffectInstance instance)
	{
		base.StartEffect(instance);
		
		GameObject target = instance.GetValue<GameObject>("target", null);
		timeManager = instance.GetContextValue<TimeManager>("timeManager", null);
		
		if (target != null)
		{
			IRootable rootable = target.GetInterfaceComponent<IRootable>();
			rootable.Root(this);
		}
	}

	public override void Cancel()
	{
		timeOfFreedom = timeManager.CurrentTime;
	}
	
	public bool StillRooted
	{
		get
		{
			return timeOfFreedom >= timeManager.CurrentTime;
		}
	}
}
