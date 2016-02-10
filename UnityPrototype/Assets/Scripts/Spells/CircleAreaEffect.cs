using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class CircleAreaEffect : AreaEffect, IFixedUpdate {

	private float radius;
	private int collideWith;
	private UpdateManager updateManager;

	private bool isUpdateAdded = false;

	private void EnsureAddedToUpdate()
	{
		if (!isUpdateAdded && updateManager != null)
		{
			this.AddToUpdateManager(updateManager);
			isUpdateAdded = true;
		}
	}
	
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		radius = instance.GetValue<float>("radius", 0.0f);
		collideWith = instance.GetValue<int>("collideWith", ~0);
		updateManager = instance.GetContextValue<UpdateManager>("updateManager", null);

		if (gameObject.activeSelf)
		{
			EnsureAddedToUpdate();
		}
	}

	public override Bounds bounds
	{
		get
		{
			return new Bounds(transform.position, Vector3.one * radius);
		}
	}

	public void OnEnable()
	{
		EnsureAddedToUpdate();
	}

	new public void OnDisable()
	{
		this.RemoveFromUpdateManager(updateManager);
		isUpdateAdded = false;

		base.OnDisable();
	}
	
	public void OnDrawGizmos()
	{
		Gizmos.DrawWireSphere(transform.position, radius);
	}

	public void FixedUpdateTick(float dt) {
		UpdateContainedShapes(index.CircleOverlap(transform.position, radius, collideWith, -1), dt);
	}
}
