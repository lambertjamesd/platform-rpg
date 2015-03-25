using UnityEngine;
using System.Collections;

public interface ITeleportable
{
	bool TeleportTo(Vector3 position);
}

public class TeleportEffect : EffectObject
{
	public override void StartEffect (EffectInstance instance)
	{
		base.StartEffect(instance);

		GameObject target = instance.GetValue<GameObject>("target", null);
		Vector3 position = instance.GetValue<Vector3>("position", target.transform.position);

		if (target != null)
		{
			ITeleportable teleportable = target.GetInterfaceComponent<ITeleportable>();

			if (teleportable != null)
			{
				if (teleportable.TeleportTo(position))
				{
					instance.TriggerEvent("success", null);
				}
				else
				{
					instance.TriggerEvent("fail", null);
				}
			}
			else
			{
				target.transform.position = position;
			}
		}
	}
}
