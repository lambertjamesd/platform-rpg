using System;
using UnityEngine;

public class ConfuseEffect : EffectObject
{
	private InputScrambler scrambler;
	private Player target;

	public override void StartEffect (EffectInstance instance)
	{
		base.StartEffect(instance);

		GameObject targetGameObject = instance.GetValue<GameObject>("target", null);

		if (targetGameObject != null)
		{
			target = targetGameObject.GetComponent<Player>();

			if (target != null)
			{
				Vector3 direction = instance.GetValue<Vector3>("rotation", Vector3.right);
				scrambler = new InputScrambler(new Vector2(direction.x, direction.y), instance.GetValue<bool>("flipX", false));
				target.AddScrambler(scrambler);
			}
		}
	}

	public override void Cancel ()
	{
		if (target != null)
		{
			target.RemoveScrambler(scrambler);
		}
	}
}
