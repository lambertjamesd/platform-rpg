using UnityEngine;
using System.Collections;

public class ShieldEffect : EffectObject, IShieldDelegate {

	private Shield shield = null;
	private Damageable damageable = null;
	
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);

		GameObject target = instance.GetValue<GameObject>("target", null);

		if (target != null)
		{
			damageable = target.GetComponent<Damageable>();
			shield = new Shield(instance.GetValue<float>("health", 0.0f), this);

			damageable.AddShield(shield);
		}
	}

	public override IEffectPropertySource PropertySource
	{
		get
		{
			return new LambdaPropertySource(parameterName => {
				switch (parameterName)
				{
				case "effect":
					return this;
				}
				
				return null;
			});
		}
	}

	public override void Cancel()
	{
		if (damageable != null)
		{
			damageable.RemoveShield(shield);
		}
	}

	public void Destroyed()
	{
		instance.TriggerEvent("destroyed", null);
	}
}
