using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LaserArea : AreaEffect, IFixedUpdate {

	public LaserVisualizer visualizer;

	private float skinThickness = 0.001f;
	private float defaultMaxRange = 100.0f;

	private Transform parentTransform;
	private UpdateManager updateManager;

	private float blockRadius;
	private int blockLayers;
	private float areaRadius;
	private int areaLayers;

	private float maxRange;
	private float lastDistance;
	
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);

		blockRadius = instance.GetValue<float>("blockRadius", 0.0f);
		blockLayers = instance.GetValue<int>("blockLayers", ~0);
		
		areaRadius = instance.GetValue<float>("areaRadius", blockRadius + skinThickness);
		areaLayers = instance.GetValue<int>("areaLayers", blockLayers);

		if (visualizer != null)
		{
			visualizer.SetRadius(areaRadius);
		}
		
		maxRange = instance.GetValue<float>("maxRange", defaultMaxRange);

		updateManager = instance.GetContextValue<UpdateManager>("updateManager", null);
		this.AddToUpdateManager(updateManager);
	}
	
	public void OnEnable() {
		this.AddToUpdateManager(updateManager);
	}
	
	new public void OnDisable() {
		this.RemoveFromUpdateManager(updateManager);

		base.OnDisable();
	}
	
	public void FixedUpdateTick (float dt) {
		
		maxRange = instance.GetValue<float>("maxRange", defaultMaxRange);
		lastDistance = maxRange;

		Ray2D ray = new Ray2D(transform.position, transform.TransformDirection(Vector3.up));
		ShapeRaycastHit hitInfo = index.Spherecast(ray, blockRadius, maxRange, -1, blockLayers);
		if (hitInfo != null)
		{
			lastDistance = hitInfo.Distance;
		}

		if (visualizer != null)
		{
			visualizer.SetInnerLength(lastDistance);
		}

		IEnumerable<ICollisionShape> hits = index.SpherecastMulti(ray, areaRadius, lastDistance, -1, areaLayers)
			.Select(x => x.Shape)
			.Concat(index.CircleOverlap(ray.origin, areaRadius, areaLayers, -1));
	
		UpdateContainedShapes(hits, dt);
	}

	public override IEffectPropertySource PropertySource
	{
		get
		{
			IEffectPropertySource baseSource = base.PropertySource;
			return new LambdaPropertySource(name => {
				switch (name)
				{
				case "length":
					return lastDistance;
				}
				return baseSource.GetObject(name);
			});
		}
	}

	public void OnDrawGizmos()
	{
		if (lastDistance < float.MaxValue)
		{
			Vector3 a = transform.position;
			Vector3 direction = transform.TransformDirection(Vector3.up);

			GizmoHelper.DrawThickLine(a, a + direction * lastDistance, blockRadius, Color.green);
			GizmoHelper.DrawThickLine(a, a + direction * lastDistance, areaRadius, new Color(1.0f, 0.5f, 0.0f));
		}
		else
		{
			Vector3 a = transform.position;
			Vector3 direction = transform.TransformDirection(Vector3.up);
			GizmoHelper.DrawSphereCast(a, direction, blockRadius, Color.green);
			GizmoHelper.DrawSphereCast(a, direction, areaRadius, new Color(1.0f, 0.5f, 0.0f));
		}
	}
}
