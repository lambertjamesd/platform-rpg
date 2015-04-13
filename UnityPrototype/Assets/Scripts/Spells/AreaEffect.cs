using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AreaEffect : EffectGameObject, ITimeTravelable {

	private HashSet<GameObject> enclosedObjects = new HashSet<GameObject>();
	private HashSet<GameObject> alreadyCollided = new HashSet<GameObject>();
	private bool firstColliderOnly = false;
	private bool noCollideRepeat = false;
	private TimeManager timeManager;
	
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);

		firstColliderOnly = instance.GetValue<bool>("firstColliderOnly", false);
		noCollideRepeat = instance.GetValue<bool>("noCollideRepeat", false);

		timeManager = instance.GetContextValue<TimeManager>("timeManager", null);
		timeManager.AddTimeTraveler(this);
	}
	
	public void OnDisable()
	{		
		foreach (GameObject gameObject in enclosedObjects)
		{
			Instance.TriggerEvent("exit", new GameObjectPropertySource(gameObject));
		}
		
		enclosedObjects.Clear();
	}

	private static IEffectPropertySource EventPropertySource(GameObject gameObject, float deltaTime)
	{
		GameObjectPropertySource gameObjectSource = new GameObjectPropertySource(gameObject);
		return new LambdaPropertySource(propertyName => {
			switch(propertyName)
			{
			case "deltaTime":
				return deltaTime;
			}
			
			return gameObjectSource.GetObject(propertyName);
		});
	}
	
	protected void UpdateContainedColliders(Collider[] colliders, float deltaTime) {


		foreach (Collider collider in colliders)
		{
			bool notFirstCollider = firstColliderOnly && alreadyCollided.Count > 0 && !alreadyCollided.Contains(collider.gameObject);
			bool alreadyEntered = noCollideRepeat && alreadyCollided.Contains(collider.gameObject) && !enclosedObjects.Contains(collider.gameObject);

			if (notFirstCollider || alreadyEntered)
			{
				continue;
			}

			if (!enclosedObjects.Contains(collider.gameObject))
			{
				enclosedObjects.Add(collider.gameObject);
				alreadyCollided.Add(collider.gameObject);
				Instance.TriggerEvent("enter", EventPropertySource(collider.gameObject, deltaTime));
			}
			else
			{
				Instance.TriggerEvent("stay", EventPropertySource(collider.gameObject, deltaTime));
			}

			if (firstColliderOnly)
			{
				break;
			}
		}

		enclosedObjects.RemoveWhere(delegate(GameObject gameObject) { 
			if (Array.Find<Collider>(colliders, collider => collider.gameObject == gameObject) == null)
			{
				Instance.TriggerEvent("exit", EventPropertySource(gameObject, deltaTime));
				return true;
			}
			else
			{
				return false;
			}
		});
	}

	public virtual object GetCurrentState()
	{
		return new object[]{
			new HashSet<GameObject>(enclosedObjects),
			new HashSet<GameObject>(alreadyCollided),
			TimeGameObject.GetCurrentState(gameObject)
		};
	}

	public virtual void RewindToState(object state)
	{
		if (state == null)
		{
			TimeGameObject.RewindToState(gameObject, null);
		}
		else
		{
			object[] stateArray = (object[])state;
			enclosedObjects = (HashSet<GameObject>)stateArray[0];
			alreadyCollided = (HashSet<GameObject>)stateArray[1];
			TimeGameObject.RewindToState(gameObject, stateArray[2]);
		}
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}
}