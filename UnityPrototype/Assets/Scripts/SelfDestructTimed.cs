using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SelfDestructTimed : MonoBehaviour, ISelfDestruct, IFixedUpdate
{
	[System.Serializable]
	public class AnimationTrigger
	{
		public string name;
		public float time;
	}

	public float zombieTime = 1.0f;
	private float currentTime = 0.0f;
	private UpdateManager updateManager;

	public List<AnimationTrigger> animationTriggers;
	private int currentAnimation = 0;
	private Animator animator;
	
	public void DestroySelf()
	{
		updateManager = gameObject.GetComponentWithAncestors<UpdateManager>();
		updateManager.AddReciever(this);
		currentTime = 0.0f;
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
			updateManager.RemoveReciever(this);
			TimeManager.DestroyGameObject(gameObject);
		}
	}
}

