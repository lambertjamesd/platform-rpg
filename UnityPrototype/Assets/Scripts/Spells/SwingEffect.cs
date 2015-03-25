using UnityEngine;
using System;

public class SwingEffect : EffectGameObject, IFixedUpdate
{
	private Vector3 startUp;
	private Vector3 finalUp;
	private float duration = 0.0f;

	private float currentTime = 0.0f;
	private UpdateManager updateManager;

	public override void StartEffect(EffectInstance instance)
	{
		base.StartEffect(instance);

		startUp = transform.TransformDirection(Vector3.up);
		finalUp = instance.GetValue("finalUp", startUp);
		duration = instance.GetValue("duration", 0.0f);
		updateManager = instance.GetContextValue<UpdateManager>("updateManager", null);
	}
	
	public void OnEnable() {
		this.AddToUpdateManager(updateManager);
	}
	
	public void OnDisable() {
		this.RemoveFromUpdateManager(updateManager);
	}
	
	public void FixedUpdateTick(float timestep)
	{
		if (currentTime < duration)
		{
			currentTime += timestep;

			Vector3 currentUp = Vector3.Slerp(startUp, finalUp, currentTime / duration);
			transform.rotation = Quaternion.LookRotation(Vector3.forward, currentUp);

			if (currentTime >= duration)
			{
				transform.rotation = Quaternion.LookRotation(Vector3.forward, finalUp);
				instance.TriggerEvent("finish", null);
			}
		}
	}
}