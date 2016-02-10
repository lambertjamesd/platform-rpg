using UnityEngine;
using System.Collections;

public class BoxObstacle : EffectGameObject {
	private CustomBox shape;

	public override void StartEffect (EffectInstance instance)
	{
		base.StartEffect (instance);
		shape = gameObject.GetOrAddComponent<CustomBox>();
		shape.offset = instance.GetValue<Vector3>("offset", Vector3.zero);
		shape.size = instance.GetValue<Vector3>("size", Vector3.one);
		shape.collisionLayers = instance.GetValue<int>("collideWith", ~0);
		shape.AddToIndex(instance.GetContextValue<SpacialIndex>("spacialIndex", null));

		gameObject.GetOrAddComponent<TimeGameObject>();
	}
}
