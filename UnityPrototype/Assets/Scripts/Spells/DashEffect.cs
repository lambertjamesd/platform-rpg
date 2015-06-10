using UnityEngine;
using System.Collections;

public interface IDashable
{
	void DashTo(Vector3 position, float speed, IDashStateDelegate dashDelegate);
}

public class DashEffect : EffectObject, IDashStateDelegate
{
	public override void StartEffect (EffectInstance instance)
	{
		base.StartEffect(instance);
		
		GameObject target = instance.GetValue<GameObject>("target", null);
		Vector3 position = instance.GetValue<Vector3>("position", target.transform.position);
		float speed = instance.GetValue<float>("speed", 1.0f);
		
		if (target != null)
		{
			IDashable dashable = target.GetInterfaceComponent<IDashable>();

			if (dashable != null)
			{
				dashable.DashTo(position, speed, this);
			}
		}
	}
	
	public void DashComplete()
	{
		instance.TriggerEvent("completed", null);
		instance.TriggerEvent("ended", null);
	}

	public void DashInterrupted()
	{
		instance.TriggerEvent("interrupted", null);
		instance.TriggerEvent("ended", null);
	}
}
