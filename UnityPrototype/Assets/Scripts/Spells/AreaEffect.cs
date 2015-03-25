using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AreaEffect : EffectGameObject {

	private HashSet<GameObject> enclosedObjects = new HashSet<GameObject>();
	
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
	}
	
	public void OnDisable()
	{		
		foreach (GameObject gameObject in enclosedObjects)
		{
			Instance.TriggerEvent("exit", new GameObjectPropertySource(gameObject));
		}
		
		enclosedObjects.Clear();
	}
	
	protected void UpdateContainedColliders(Collider[] colliders) {
		foreach (Collider collider in colliders)
		{
			if (!enclosedObjects.Contains(collider.gameObject))
			{
				enclosedObjects.Add(collider.gameObject);
				Instance.TriggerEvent("enter", new GameObjectPropertySource(collider.gameObject));
			}
			else
			{
				Instance.TriggerEvent("stay", new GameObjectPropertySource(collider.gameObject));
			}
		}
		
		if (colliders.Length < enclosedObjects.Count)
		{
			enclosedObjects.RemoveWhere(delegate(GameObject gameObject) { 
				if (Array.Find<Collider>(colliders, collider => collider.gameObject == gameObject) == null)
				{
					Instance.TriggerEvent("exit", new GameObjectPropertySource(gameObject));
					return true;
				}
				else
				{
					return false;
				}
			});
		}
	}
}