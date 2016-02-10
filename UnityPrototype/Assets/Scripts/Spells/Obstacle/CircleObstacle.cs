using UnityEngine;
using System.Collections;

public class CircleObstacle : EffectGameObject {
	private CustomCircle shape;

	public override void StartEffect (EffectInstance instance)
	{
		base.StartEffect (instance);
		shape = gameObject.GetOrAddComponent<CustomCircle>();
		shape.offset = instance.GetValue<Vector3>("offset", Vector3.zero);
		shape.radius = instance.GetValue<float>("radius", 1.0f);
		shape.collisionLayers = instance.GetValue<int>("collideWith", ~0);
		shape.AddToIndex(instance.GetContextValue<SpacialIndex>("spacialIndex", null));
		
		gameObject.GetOrAddComponent<TimeGameObject>();
	}
}
