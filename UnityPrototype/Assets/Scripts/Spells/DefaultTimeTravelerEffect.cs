using UnityEngine;
using System.Collections;

public class DefaultTimeTravelerEffect : EffectGameObject {
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		gameObject.GetOrAddComponent<TimeGameObject>();
	}
}
