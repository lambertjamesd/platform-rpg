﻿using UnityEngine;
using System.Collections;

public class TimeGameObject : MonoBehaviour, ITimeTravelable {

	private TimeManager timeManager;

	public void Awake()
	{
		timeManager = gameObject.GetComponentWithAncestors<TimeManager>();
	}

	public void OnEnable()
	{
		timeManager.AddTimeTraveler(this);
	}

	public void OnDisable()
	{

	}
	
	public object GetCurrentState()
	{
		return GetCurrentState(gameObject);
	}
	
	public void RewindToState(object state)
	{
		RewindToState(gameObject, state);
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}

	
	private class GameObjectData
	{
		public Transform parent;
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 scale;
	}

	public static object GetCurrentState(GameObject gameObject)
	{
		if (gameObject.activeSelf)
		{
			GameObjectData result = new GameObjectData();
			result.parent = gameObject.transform.parent;
			result.position = gameObject.transform.localPosition;
			result.rotation = gameObject.transform.localRotation;
			result.scale = gameObject.transform.localScale;
			return result;
		}
		else
		{
			return null;
		}
	}

	public static void RewindToState(GameObject gameObject, object state)
	{
		if (state != null)
		{
			GameObjectData data = (GameObjectData)state;
			gameObject.transform.parent = data.parent;
			gameObject.transform.localPosition = data.position;
			gameObject.transform.localRotation = data.rotation;
			gameObject.transform.localScale = data.scale;
			gameObject.SetActive(true);
		}
		else
		{
			Destroy(gameObject);
		}
	}
}
