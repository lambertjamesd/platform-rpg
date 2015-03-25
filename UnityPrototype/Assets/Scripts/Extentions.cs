using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class Extentions 
{
	public static I GetInterfaceComponent<I>(this GameObject gameObject) where I : class
	{
		return gameObject.GetComponent(typeof(I)) as I;
	}

	public static IList<I> GetInterfaceComponents<I>(this GameObject gameObject) where I : class
	{
		MonoBehaviour[] monoBehaviours = gameObject.GetComponents<MonoBehaviour>();
		List<I> result = new List<I>();
		
		foreach(MonoBehaviour behaviour in monoBehaviours)
		{
			if(behaviour is I)
			{
				result.Add(behaviour as I);
			}
		}
		
		return result;
	}

	public static GameObject GetParent(this GameObject gameObject)
	{
		if (gameObject.transform.parent == null)
		{
			return null;
		}
		else
		{
			return gameObject.transform.parent.gameObject;
		}
	}

	public static I GetOrAddComponent<I>(this GameObject gameObject) where I : Component
	{
		I result = gameObject.GetComponent<I>();

		if (result == null)
		{
			result = gameObject.AddComponent<I>();
		}

		return result;
	}

	public static I GetComponentWithAncestors<I>(this GameObject gameObject) where I : Component
	{
		I result = null;
		GameObject currentObject = gameObject;

		while (result == null && currentObject != null)
		{
			result = currentObject.GetComponent<I>();
			currentObject = currentObject.GetParent();
		}

		return result;
	}
}
