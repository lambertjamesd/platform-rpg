using UnityEngine;
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
		timeManager.RemoveTimeTraveler(this);
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
		public bool active;
		public Transform parent;
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 scale;
	}

	public static object GetCurrentState(GameObject gameObject)
	{
		GameObjectData result = new GameObjectData();
		result.active = gameObject.activeSelf;
		result.parent = gameObject.transform.parent;
		result.position = gameObject.transform.localPosition;
		result.rotation = gameObject.transform.localRotation;
		result.scale = gameObject.transform.localScale;
		return result;
	}

	public static void RewindToState(GameObject gameObject, object state)
	{
		if (state != null)
		{
			GameObjectData data = (GameObjectData)state;
			gameObject.SetActive(data.active);

			gameObject.transform.parent = data.parent;
			gameObject.transform.localPosition = data.position;
			gameObject.transform.localRotation = data.rotation;
			gameObject.transform.localScale = data.scale;
		}
		else
		{
			gameObject.SetActive(false);
		}
	}
}
