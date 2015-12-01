using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AreaEffectExitListener : EffectGameObject, IOnExitDelegate {

	private AreaEffect areaEffect;
	private GameObject target;

	public override void StartEffect(EffectInstance instance)
	{
		base.StartEffect(instance);

		areaEffect = instance.GetValue<AreaEffect>("areaEffect", null);
		target = instance.GetValue<GameObject>("target", null);

		if (areaEffect != null && target != null)
		{
			areaEffect.AddExitDelegate(target, this);
		}
	}

	public override void Cancel ()
	{
		if (areaEffect != null)
		{
			areaEffect.RemoveExitDelegate(target, this);
		}
	}
	
	public void OnExit(IEffectPropertySource propertySource)
	{
		instance.TriggerEvent("exit", propertySource);
	}
}

public interface IOnExitDelegate {
	void OnExit(IEffectPropertySource propertySource);
}

public class AreaEffect : EffectGameObject, ITimeTravelable {

	private HashSet<GameObject> enclosedObjects = new HashSet<GameObject>();
	private HashSet<GameObject> alreadyCollided = new HashSet<GameObject>();
	private bool firstColliderOnly = false;
	private bool noCollideRepeat = false;
	private TimeManager timeManager;

	private Dictionary<GameObject, List<IOnExitDelegate>> exitListeners = new Dictionary<GameObject, List<IOnExitDelegate>>();

	public void AddExitDelegate(GameObject listenFor, IOnExitDelegate exitDelegate)
	{
		if (exitListeners.ContainsKey(listenFor))
		{
			exitListeners[listenFor].Add(exitDelegate);
		}
		else
		{
			List<IOnExitDelegate> delegates = new List<IOnExitDelegate>();
			delegates.Add(exitDelegate);
			exitListeners[listenFor] = delegates;
		}
	}

	public void RemoveAllExitDelegates(GameObject listenFor, IEffectPropertySource properties = null)
	{
		if (exitListeners.ContainsKey(listenFor))
		{
			List<IOnExitDelegate> delegates = exitListeners[listenFor];
			
			properties = properties ?? EventPropertySource(listenFor, 0.0f, this);
			while (delegates.Count > 0)
			{
				RemoveExitDelegate(listenFor, delegates[0], properties);
			}
		}
	}
	
	public void RemoveExitDelegate(GameObject listenFor, IOnExitDelegate exitDelegate, IEffectPropertySource properties = null)
	{
		if (exitListeners.ContainsKey(listenFor))
		{
			properties = properties ?? EventPropertySource(listenFor, 0.0f, this);
			exitDelegate.OnExit(properties);
			exitListeners[listenFor].Remove(exitDelegate);
		}
	}
	
	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);

		firstColliderOnly = instance.GetValue<bool>("firstColliderOnly", false);
		noCollideRepeat = instance.GetValue<bool>("noCollideRepeat", false);

		timeManager = instance.GetContextValue<TimeManager>("timeManager", null);
		timeManager.AddTimeTraveler(this);
	}

	private void ExitEvent(GameObject target, float deltaTime)
	{
		IEffectPropertySource properties = EventPropertySource(target, deltaTime, this);
		RemoveAllExitDelegates(target, properties);
		Instance.TriggerEvent("exit", properties);
	}

	public void OnDisable()
	{		
		foreach (GameObject gameObject in enclosedObjects)
		{
			ExitEvent(gameObject, 0.0f);
		}
		
		enclosedObjects.Clear();
	}

	private static IEffectPropertySource EventPropertySource(GameObject gameObject, float deltaTime, AreaEffect area)
	{
		GameObjectPropertySource gameObjectSource = new GameObjectPropertySource(gameObject, area);
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
				Instance.TriggerEvent("enter", EventPropertySource(collider.gameObject, deltaTime, this));
			}
			else
			{
				Instance.TriggerEvent("stay", EventPropertySource(collider.gameObject, deltaTime, this));
			}

			if (firstColliderOnly)
			{
				break;
			}
		}

		enclosedObjects.RemoveWhere(delegate(GameObject gameObject) { 
			if (Array.Find<Collider>(colliders, collider => collider.gameObject == gameObject) == null)
			{
				ExitEvent(gameObject, deltaTime);
				return true;
			}
			else
			{
				return false;
			}
		});
	}

	public static Dictionary<GameObject,List<IOnExitDelegate>> DuplicateListeners(Dictionary<GameObject,List<IOnExitDelegate>> source)
	{
		Dictionary<GameObject,List<IOnExitDelegate>> listenerCopy = new Dictionary<GameObject,List<IOnExitDelegate>>(source.Count);
		
		foreach (KeyValuePair<GameObject,List<IOnExitDelegate>> keypair in source)
		{
			listenerCopy.Add(keypair.Key, new List<IOnExitDelegate>(keypair.Value));
		}

		return listenerCopy;
	}

	public virtual object GetCurrentState()
	{
		object result = TimeGameObject.GetCurrentState(gameObject);

		if(result == null)
		{
			return null;
		}
		else
		{
			return new object[]{
				new HashSet<GameObject>(enclosedObjects),
				new HashSet<GameObject>(alreadyCollided),
				result,
				DuplicateListeners(exitListeners)
			};
		}
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
			enclosedObjects = new HashSet<GameObject>((HashSet<GameObject>)stateArray[0]);
			alreadyCollided = new HashSet<GameObject>((HashSet<GameObject>)stateArray[1]);
			TimeGameObject.RewindToState(gameObject, stateArray[2]);
			exitListeners = DuplicateListeners((Dictionary<GameObject,List<IOnExitDelegate>>)stateArray[3]);
		}
	}

	public virtual Bounds bounds
	{
		get
		{
			return new Bounds(transform.position, Vector3.zero);
		}
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}

	public override IEffectPropertySource PropertySource {
		get {
			IEffectPropertySource baseSource = base.PropertySource;

			return new LambdaPropertySource(name => {
				switch (name) {
				case "enclosedObjects":
					return enclosedObjects.Cast<object>().ToList();
				case "alreadyCollided":
					return alreadyCollided.Cast<object>().ToList();
				}

				return baseSource.GetObject(name);
			});
		}
	}
}