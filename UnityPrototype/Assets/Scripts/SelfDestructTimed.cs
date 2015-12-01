using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SelfDestructTimed : MonoBehaviour, ISelfDestruct, IFixedUpdate, ITimeTravelable
{
	[System.Serializable]
	public class AnimationTrigger
	{
		public string name;
		public float time;
	}

	public float zombieTime = 1.0f;
	private float currentTime = 0.0f;
	private bool addedToUpdateManager;
	private UpdateManager updateManager;
	
	private TimeManager timeManager;

	public List<AnimationTrigger> animationTriggers;
	private int currentAnimation = 0;
	private Animator animator;
	
	private void AddToUpdate()
	{
		if (!addedToUpdateManager)
		{
			this.AddToUpdateManager(updateManager);
			addedToUpdateManager = true;
		}
	}
	
	private void RemoveFromUpdate()
	{
		if (addedToUpdateManager)
		{
			this.RemoveFromUpdateManager(updateManager);
			addedToUpdateManager = false;
		}
	}
	
	public void DestroySelf()
	{
		updateManager = gameObject.GetComponentWithAncestors<UpdateManager>();
		timeManager = gameObject.GetComponentWithAncestors<TimeManager>();
		timeManager.AddTimeTraveler(this);
		AddToUpdate();
		currentTime = 0.0f;
		currentAnimation = 0;
		animator = GetComponent<Animator>();
	}
	
	public void FixedUpdateTick(float timestep)
	{
		currentTime += timestep;

		while (currentAnimation < animationTriggers.Count && currentTime >= animationTriggers[currentAnimation].time)
		{
			animator.SetTrigger(animationTriggers[currentAnimation].name);
			++currentAnimation;
		}
		
		if (currentTime >= zombieTime)
		{
			RemoveFromUpdate();
			TimeManager.DestroyGameObject(gameObject);
		}
	}
	
	public object GetCurrentState()
	{
		if (addedToUpdateManager)
		{
			return new object[]{
				currentTime,
				currentAnimation
			};
		}
		else
		{
			return null;
		}
	}
	
	public void RewindToState(object state)
	{
		if (state == null)
		{
			RemoveFromUpdate();
		}
		else
		{
			object[] stateValues = (object[])state;

			currentTime = (float)stateValues[0];
			currentAnimation = (int)stateValues[1];
			
			if (currentTime > 0.0f)
			{
				AddToUpdate();
			}
		}
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}
}

