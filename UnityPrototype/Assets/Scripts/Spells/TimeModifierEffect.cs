using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TimeModifierEffect : EffectObject
{
	private List<IFixedUpdate> updateTargets;
	private UpdateManager updateManager;
	private UpdateSpeedModifier modifier;

	public override void StartEffect (EffectInstance instance)
	{
		base.StartEffect(instance);

		GameObject target = instance.GetValue<GameObject>("target", null);
		updateTargets = target.GetInterfaceComponents<IFixedUpdate>();
		modifier = new UpdateSpeedModifier(instance.GetValue<float>("timeScale", 1.0f));

		updateManager = instance.GetContextValue<UpdateManager>("updateManager", null);

		foreach (IFixedUpdate updateTarget in updateTargets)
		{
			updateManager.AddUpdateModifier(updateTarget, modifier);
		}
	}

	public override void Cancel()
	{
		foreach (IFixedUpdate updateTarget in updateTargets)
		{
			updateManager.RemoveUpdateModifier(updateTarget, modifier);
		}
	}
}
