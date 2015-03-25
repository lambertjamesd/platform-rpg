using UnityEngine;
using System.Collections;

public class RectangleBarrier : EffectGameObject {

	private BoxCollider boxCollider;

	public override void StartEffect (EffectInstance instance)
	{
		base.StartEffect (instance);

		boxCollider = gameObject.GetOrAddComponent<BoxCollider>();
		boxCollider.size = instance.GetValue<Vector3>("size", boxCollider.size);
	}
}
