using UnityEngine;
using System.Collections;

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

		lastDistance = maxRange;

		Ray ray = new Ray(transform.position, transform.TransformDirection(Vector3.up));
		RaycastHit hitInfo;
		if (Physics.SphereCast(ray, blockRadius, out hitInfo, maxRange, blockLayers))
		{
			lastDistance = hitInfo.distance;
		}

		if (visualizer != null)
		{
			visualizer.SetInnerLength(lastDistance);
		}

		RaycastHit[] hits = Physics.SphereCastAll(ray, areaRadius, lastDistance, areaLayers);

		Collider[] colliderList = new Collider[hits.Length];

		for (int i = 0; i < hits.Length; ++i)
		{
			colliderList[i] = hits[i].collider;
		}

		UpdateContainedColliders(colliderList, dt);
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
