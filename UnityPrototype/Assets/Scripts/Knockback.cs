using UnityEngine;
using System.Collections;

public interface IKnockbackReciever {
	void Knockback(Vector3 impulse);
}

public class Knockback : EffectObject  {
	public override void StartEffect(EffectInstance instance)
	{
		base.StartEffect(instance);
		float strength = instance.GetValue<float>("strength", 0.0f);
		Vector3 direction = instance.GetValue<Vector3>("direction", Vector3.zero).normalized;
		GameObject target = instance.GetValue<GameObject>("target", null);

		if (target != null)
		{
			IKnockbackReciever reviever = target.GetInterfaceComponent<IKnockbackReciever>();

			if (reviever != null)
			{
				reviever.Knockback(direction * strength);
			}
		}	
	}
}
